using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Server.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    /// <summary>
     /// Dictionary of warp points with unique identifiers.
     /// </summary>
     private Dictionary<string, EntityUid> warpPoints = new Dictionary<string, EntityUid>();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
    }

    public EntityUid? FindWarpPoint(string id)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var found = entMan.EntityQuery<WarpPointComponent>(true).Where(p => p.ID == id).FirstOrDefault();
        if (found is not null)
            return found.Owner;
        else
            return null;
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner))
            return;

        var loc = component.Location == null ? "<null>" : $"'{component.Location}'";
        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
    }
}
