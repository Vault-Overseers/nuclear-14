namespace Content.Server.Mind.Components
{
    [RegisterComponent]
    public sealed class VisitingMindComponent : Component
    {
        [ViewVariables]
        public Mind? Mind;
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
