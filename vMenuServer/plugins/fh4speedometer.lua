AddEventHandler("vMenu:FH4SpeedOMeter", function(cb)
    TriggerEvent("FH4:FH4SpeedOMeter", function(Val)
        cb(Val and ConfigShared.FH4SpeedOMeter)
    end) 
end)