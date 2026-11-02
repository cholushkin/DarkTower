namespace Game
{
    /// Implemented by content definitions that carry a stable, designer-authored string id.
    /// Catalogs and validation iterate definitions by this id; gameplay persistence must not
    /// depend on Unity object instance ids.
    public interface IIdentifiedDefinition
    {
        string Id { get; }
    }
}
