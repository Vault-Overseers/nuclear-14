using Robust.Shared.Configuration;

namespace Content.KayMisaZlevels.Shared;

[CVarDefs]
public static class KMZLevelsCVars
{
    public static readonly CVarDef<bool> ProcessAllPhysicsObjects = CVarDef.Create("kzlevels.process_all_physics_objects",
        true,
        CVar.SERVER | CVar.REPLICATED,
        "Whether or not KZLevels should process all object movement instead of only KZPhysicsComponent marked objects. Only settable during early startup, midgame changes have no effect.");
}
