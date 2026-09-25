using System;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    [Serializable] public sealed class CardArtLayer
    {
        public string texture="";
        public string fit="cover";
        [Range(-1,1)] public float offsetX,offsetY;
        [Range(.25f,3)] public float zoom=1.08f;
        [Range(0,.2f)] public float depth;
        [Range(0,1)] public float foil;
    }
    [Serializable] public sealed class CardSubjectPlane
    {
        [Range(-1,1)] public float offsetX,offsetY;
        [Range(.25f,3)] public float zoom=1;
        [Range(0,.03f)] public float depth=.006f;
        [Range(0,1)] public float foil=.24f;
        // Source placements are static inside one plane, never independent parallax layers.
        public CardArtLayer rear=new CardArtLayer();
        public CardArtLayer foreground=new CardArtLayer();
    }
    [Serializable] public sealed class LayeredCardArt
    {
        public string cardName="";
        [Range(1,3)] public float responsePower=1.5f;
        public CardArtLayer background=new CardArtLayer{zoom=1.3f,depth=.085f};
        public CardSubjectPlane subject=new CardSubjectPlane();
    }
    [Serializable] public sealed class LayeredCardsConfig
    {
        public int schemaVersion=2;
        public LayeredCardArt[] cards=Array.Empty<LayeredCardArt>();
        public LayeredCardArt Find(string name)=>cards.FirstOrDefault(c=>string.Equals(c.cardName,name,StringComparison.Ordinal));
        public void Validate(Catalog catalog)
        {
            if(schemaVersion!=2||cards==null)throw new FormatException("layered-cards: schemaVersion 2 required (background + combined subject)");
            if(cards.Select(c=>c.cardName).Distinct(StringComparer.Ordinal).Count()!=cards.Length)throw new FormatException("layered-cards: duplicate card name");
            foreach(var card in cards)
            {
                if(catalog.cards.Count(c=>c.name==card.cardName)!=1)throw new FormatException("layered-cards: expected one card named "+card.cardName);
                if(card.subject==null)throw new FormatException("layered-cards: combined subject required");
                foreach(var layer in new[]{card.background,card.subject.rear,card.subject.foreground})
                {
                    if(layer==null||string.IsNullOrWhiteSpace(layer.texture)||!layer.texture.StartsWith("LayeredCards/Layers/")||layer.texture.Contains("..")||Resources.Load<Texture2D>(layer.texture)==null)
                        throw new FormatException("layered-cards: unknown texture for "+card.cardName);
                    if(layer.fit!="cover"&&layer.fit!="contain")throw new FormatException("layered-cards: fit must be cover or contain");
                    Range(layer.offsetX,-1,1);Range(layer.offsetY,-1,1);Range(layer.zoom,.25f,3);Range(layer.depth,0,.2f);Range(layer.foil,0,1);
                }
                var subject=card.subject;
                Range(card.responsePower,1,3);Range(subject.offsetX,-1,1);Range(subject.offsetY,-1,1);Range(subject.zoom,.25f,3);Range(subject.depth,0,.03f);Range(subject.foil,0,1);
                if(card.background.foil!=0)throw new FormatException("layered-cards: background has no foil");
                foreach(var part in new[]{subject.rear,subject.foreground})
                    if(part.depth!=0||part.foil!=0)throw new FormatException("layered-cards: subject sources cannot move or shimmer independently; use subject.depth/foil");
            }
        }
        static void Range(float value,float min,float max){if(!float.IsFinite(value)||value<min||value>max)throw new FormatException("layered-cards: parameter outside range");}
    }
    public static class LayeredArtMaterial
    {
        public static Material Create(bool world=false)
        {
            var shader=Resources.Load<Shader>(world?"LayeredCards/LayeredCardWorld":"LayeredCards/LayeredCardUI");
            if(shader==null)throw new InvalidOperationException("Layered card shader missing");
            return new Material(shader){hideFlags=HideFlags.DontSave};
        }
        public static void Configure(Material material,LayeredCardArt art,float aspect)
        {
            material.SetFloat("_WindowAspect",Mathf.Max(.01f,aspect));
            Apply("Background",art.background);Apply("Rear",art.subject.rear);Apply("Foreground",art.subject.foreground);
            material.SetVector("_Subject",new Vector4(art.subject.offsetX,art.subject.offsetY,art.subject.zoom,art.subject.depth));
            material.SetFloat("_SubjectFoil",art.subject.foil);material.SetFloat("_ResponsePower",art.responsePower);
            void Apply(string key,CardArtLayer layer)
            {
                var texture=Resources.Load<Texture2D>(layer.texture);
                material.SetTexture("_"+key+"Tex",texture);
                material.SetVector("_"+key,new Vector4(layer.offsetX,layer.offsetY,layer.zoom,layer.depth));
                material.SetVector("_"+key+"Info",new Vector4((float)texture.width/texture.height,layer.fit=="cover"?1:0,layer.foil,0));
            }
        }
        public static void View(Material material,Vector2 look){material.SetVector("_ViewOffset",new Vector4(look.x,look.y,0,0));}
    }
}
