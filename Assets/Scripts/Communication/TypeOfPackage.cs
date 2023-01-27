namespace Communication
{
    
    // All types of packages
    public enum TypeOfPackage : byte
    {
        Subsystem,
        AddGuidance,
        RemoveGuidance,
        CreateOrMove,
        SetHouseOffset,
        RemoveHouse,
        SetHouseShared,
        SetHouseRotation,
        SetHouseScale,
        HouseAddBlock,
        HouseRemoveBlock,
        HouseAimMoved,
        CopyHouse,
        PlayerIDs,
        StalkAction,
        ForceStalkAction,
        ReplaceAction,
        CreateNewGeofence,
        LoadWorld,
        RequestLoadWorld,
        ScaleRotateGeofence,
        SetPlotSizeAction,
        DistributeFreezeFrame,
        AddLineSegment,
        RemoveLine,
        RemoveDrawing
    }
}