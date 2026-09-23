namespace SummonersTable
{
    public static class CardPresentationContext
    {
        public static MatchOptions Options {get;private set;}=new MatchOptions();
        public static void Apply(MatchOptions options){Options=options??new MatchOptions();}
    }
}
