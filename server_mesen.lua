-------------------------------------
--  Advance View Mesen Serverside  --
--         By Antimattur           --
-------------------------------------

---------------------------------------------------
-- Constants / Memory addresses (Region Specific) --
---------------------------------------------------
local addr = {
    sceneStateFn    = 0x3002e20,
    raceMain        = 0x805167D,
    trackHeader     = 0x30033B8,
    trackLoadFn     = 0x8031e08,
    tiles           = 0x6008000,
    trackmap        = 0x2010400,
    halfTrackSize   = 0x3002F30,
    palette         = 0x5000000,
    raceState       = 0x3002E44,
    aiMap           = 0x2025400,
}
addr.driverCount     = addr.raceState + 0x7F0
addr.drivers         = addr.raceState + 0x7F4
addr.playerDriver    = addr.raceState + 0x814

-----------------------------------------------
-- Emulator-Dependent Functions (Mesen ver.) --
-----------------------------------------------

local function ReadU32(addr)
    return emu.read32(addr, emu.memType.gbaDebug, false)
end
local function ReadU8(addr)
    return emu.read(addr, emu.memType.gbaDebug, false)
end
local function Log(str)
    emu.log(str)
end

-- Define functions for later callback
local Main
local Close
local SendTrackLoaded
local function SetupCallbacks()
	emu.addEventCallback(Main, emu.eventType.endFrame)
	emu.addMemoryCallback(SendTrackLoaded, emu.callbackType.exec, addr.trackLoadFn)
	emu.addEventCallback(Close, emu.eventType.scriptEnded)
end


--------------------------------------
-- Driver struct field offsets      --
--------------------------------------
local driverOffsets = {
    posX = 0x0,
    posY = 0x4,
    checkpoint = 0x38
    -- TODO: add other offsets
}

-------------------
-- Enum Packets  --
-------------------
local packetType = {
    trackLoaded = 0,
    trackUnloaded = 1,
    tileGfx = 2,
    trackMap = 3,
    palette = 4,
    driver = 5,
    aiMap = 6,
    behaviors = 7,
}

---------------------------
-- Required Dependencies --
---------------------------
local socket = require("socket.core")

------------------
-- Global State --
------------------
local loadedTrack = nil
local server = socket.tcp()
local client = nil

----------------------------
-- Socket Initialization --
----------------------------
local bindSuccess, bindErr = server:bind("127.0.0.1", 34977)
if not bindSuccess then
	Log("Could not bind server port. Please make sure the Advance View client isn't running and try agin. If there is still an error, try restarting the emulator as well.")
	return
end
assert(server:listen(1))
server:settimeout(0)

-----------------------------------
-- Driver data helper functions --
-----------------------------------
local function GetDriverAddr(driver)
    return ReadU32(addr.drivers + 4*driver)
end
local function GetPlayerDriver()
    return ReadU32(addr.playerDriver)
end
local function GetDriverData(driverAddr)
    return {
        x =  ReadU32(driverAddr + driverOffsets.posX),
        y =  ReadU32(driverAddr + driverOffsets.posY),
        checkpoint = ReadU8(driverAddr + driverOffsets.checkpoint),
    }
end

-----------------------
-- Utility Functions --
-----------------------

local function int32_le(n)
    if not n then n = 0 end
    return string.char(
        n & 0xFF,
        (n >> 8) & 0xFF,
        (n >> 16) & 0xFF,
        (n >> 24) & 0xFF
    )
end

local function GetMemBlock(start, len)
    local buffer = {}
    local i = 0
    while i + 4 <= len do
        local word = ReadU32(start+i)
        buffer[#buffer+1] = string.char(
            (word >> 0) & 0xFF,
            (word >> 8) & 0xFF,
            (word >> 16) & 0xFF,
            (word >> 24) & 0xFF
        )
        i = i + 4
    end
    return table.concat(buffer)
end

local function HandleSendResults(success, err)
    if not success then
        if err == "closed" then
            Log("Client disconnected")
            loadedTrack = nil
            client = nil
        end
    end
end

local function SendHeaderedData(dataType, data)
    if not client then return nil, "clientnil" end
    local header = int32_le(578)..int32_le(dataType)..int32_le(data:len())
    local message = header .. data
    return client:send(message)
end

-----------------------
-- Data Transmission --
-----------------------

local function SendPalette()
    -- Send palette
    local paletteData = GetMemBlock(addr.palette, 512)
    local success, err = SendHeaderedData(packetType.palette, paletteData)
    HandleSendResults(success, err)
end

local function SendGraphics()
    -- Send tile gfx
    local tileData = GetMemBlock(addr.tiles, 0x4000)
    local success, err = SendHeaderedData(packetType.tileGfx, tileData)
    HandleSendResults(success, err)

    -- Send tile behaviors
    local trackHeader = ReadU32(addr.trackHeader)
    local behaviors = GetMemBlock(trackHeader + ReadU32(trackHeader+136), 256)
    local success, err = SendHeaderedData(packetType.behaviors, behaviors)
    HandleSendResults(success, err)

    SendPalette()

    -- Send map layout
    local mapSize = ReadU32(addr.halfTrackSize) * 2
    local mapData = GetMemBlock(addr.trackmap, mapSize * mapSize)
    success, err = SendHeaderedData(packetType.trackMap, mapData)
    HandleSendResults(success, err)

    local aiMapSize = mapSize/2
    local aiMapData = GetMemBlock(addr.aiMap, aiMapSize * aiMapSize)
    success, err = SendHeaderedData(packetType.aiMap, aiMapData)
    HandleSendResults(success, err)
end

local function CreateDriverPacket(driver)
    return int32_le(driver.x)..int32_le(driver.y)
end

local function SendPlayer()
    local playerData = GetDriverData(GetPlayerDriver())
    local packetData = CreateDriverPacket(playerData)
    local success, err = SendHeaderedData(packetType.driver, packetData)
    HandleSendResults(success, err)
end

function SendTrackLoaded()
    if not client then return end
    local success, err = SendHeaderedData(packetType.trackLoaded, "load")
    HandleSendResults(success, err)
    SendGraphics()
end

local function SendClient()
    SendPalette()
    SendPlayer()
end

--------------------
-- Frame Callback --
--------------------

function Main()
    if not client then
        local c, err = server:accept()
        if c then
            client = c
            client:settimeout(0)
            Log("Client connected")
        elseif err ~= "timeout" then
            Log("Accept error: " .. tostring(err))
        end
    else
        SendClient()
    end
end

-------------------------------
-- Shutdown and Cleanup      --
-------------------------------

function Close()
    if client then client:close() end
    if server then server:close() end
end

-------------------------------
-- Emulator Event Bindings   --
-------------------------------

SetupCallbacks()

Log("Listening on port 34977")
