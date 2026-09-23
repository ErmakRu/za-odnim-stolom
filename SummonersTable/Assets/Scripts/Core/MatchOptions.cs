using System;
namespace SummonersTable
{
    [Serializable] public sealed class MatchOptions
    {
        public const string Wizards="wizards",Commanders="commanders";
        public string mode=Wizards;
        public bool limitPower=true,cards3D;
        public int revision;
        public bool IsCommanders=>mode==Commanders;
        public string ModeName=>IsCommanders?"Полководцы":"Волшебники";
        public string Signature=>revision+":"+mode+":"+(limitPower?1:0)+":"+(cards3D?1:0);
        public MatchOptions Copy(){return (MatchOptions)MemberwiseClone();}
        public void Validate(){if(mode!=Wizards&&mode!=Commanders||revision<0)throw new ArgumentException("Invalid lobby rules.");}
        public bool SameRules(MatchOptions other){return other!=null&&mode==other.mode&&limitPower==other.limitPower&&cards3D==other.cards3D;}
    }
}
