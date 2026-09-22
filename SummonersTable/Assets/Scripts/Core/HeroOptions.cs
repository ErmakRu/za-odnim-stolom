using System;
namespace SummonersTable
{
    public static class HeroOptions
    {
        public static readonly string[] Ids={"badger","deer","dog","lion","lizard","owl","rabbit","rat"};
        public static readonly string[] Names={"Барсук","Олень","Пёс","Лев","Ящер","Сова","Кролик","Крыс"};
        public static bool Valid(string id){return Array.IndexOf(Ids,id)>=0;}
        public static string Normalize(string id){return Valid(id)?id:Ids[0];}
        public static int Outfit(int value){return Math.Max(0,Math.Min(7,value));}
        public static int Palette(int value){return Math.Max(0,Math.Min(3,value));}
    }
}
