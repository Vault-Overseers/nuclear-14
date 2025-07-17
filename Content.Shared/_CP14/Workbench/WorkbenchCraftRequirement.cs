/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CP14.Workbench;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CP14WorkbenchCraftRequirement
{
    /// <summary>
    /// Here a check is made that the recipe as a whole can be fulfilled at the current moment. Do not add anything that affects gameplay here, and only perform checks here.
    /// </summary>
    /// <returns></returns>
    public abstract bool CheckRequirement(EntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities);

    /// <summary>
    /// An event that is triggered after crafting. This is the place to put important things like removing items, spending stacks or other things.
    /// </summary>
    public virtual void PostCraft(EntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {

    }

    public virtual double GetPrice(EntityManager entManager,
        IPrototypeManager protoManager)
    {
        return 0;
    }

    /// <summary>
    /// This text will be displayed in the description of the craft recipe. Write something like ‘Wooden planks: х10’ here
    /// </summary>
    public virtual string GetRequirementTitle(IPrototypeManager protoManager)
    {
        return string.Empty;
    }

    /// <summary>
    /// You can specify an icon generated from an entity. It will support layering, colour changes and other layer options. Return null to disable.
    /// </summary>
    public virtual EntityPrototype? GetRequirementEntityView(IPrototypeManager protoManager)
    {
        return null;
    }

    /// <summary>
    /// You can specify the texture directly. Return null to disable.
    /// </summary>
    public virtual SpriteSpecifier? GetRequirementTexture(IPrototypeManager protoManager)
    {
        return null;
    }

    /// <summary>
    /// Optional, allows you to repaint the icon.
    /// </summary>
    public virtual Color GetRequirementColor(IPrototypeManager protoManager)
    {
        return Color.White;
    }
}
