using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class CampaignPresentationMigration
    {
        public static void Apply()
        {
            var g=PrefabUtility.LoadPrefabContents(CampaignAuthoring.Prefab);
            try
            {
                var v=g.GetComponent<CampaignComicView>();var parent=v.comic.transform;
                var viewport=parent.Find("Actor stage");
                if(viewport==null){viewport=SharedPresentationMigration.Add<RectMask2D>("Actor stage",parent,0,155,1600,690).transform;viewport.SetSiblingIndex(v.left.transform.GetSiblingIndex());}
                v.left.transform.SetParent(viewport,false);v.right.transform.SetParent(viewport,false);v.rightBorder.gameObject.SetActive(false);
                v.leftName.gameObject.SetActive(false);v.rightName.gameObject.SetActive(false);
                parent.Find("Dialogue frame").gameObject.SetActive(false);parent.Find("Dialogue body").gameObject.SetActive(false);
                if(v.dialoguePanel==null)v.dialoguePanel=SharedPresentationMigration.Add<TavernPanel>("Dialogue plate",parent,0,-310,1504,290);
                v.dialoguePanel.raycastTarget=false;v.dialoguePanel.corner=24;v.dialoguePanel.inset=4;v.dialoguePanel.transform.SetSiblingIndex(viewport.GetSiblingIndex()+1);
                if(v.namePanel==null)v.namePanel=SharedPresentationMigration.Add<TavernPanel>("Speaker plate",parent,-522,-165,404,58);
                v.namePanel.corner=13;v.namePanel.raycastTarget=false;v.namePanel.transform.SetSiblingIndex(v.dialoguePanel.transform.GetSiblingIndex()+1);
                SharedPresentationMigration.Place(v.speaker.transform,-522,-165,360,42);v.speaker.alignment=TextAnchor.MiddleCenter;v.speaker.color=new Color(1,.95f,.84f);v.speaker.fontSize=27;v.speaker.transform.SetAsLastSibling();
                SharedPresentationMigration.Place(v.body.transform,0,-281,1408,142);v.body.fontSize=34;v.body.lineSpacing=1.18f;v.body.alignment=TextAnchor.UpperLeft;v.body.transform.SetAsLastSibling();
                var clicks=parent.GetComponentInChildren<ComicClickSurface>(true);
                if(clicks==null){var hit=SharedPresentationMigration.Add<Image>("Click to read",parent,0,0,1600,1000);hit.color=Color.clear;hit.raycastTarget=true;hit.transform.SetSiblingIndex(viewport.GetSiblingIndex());clicks=hit.gameObject.AddComponent<ComicClickSurface>();}clicks.view=v;
                if(v.rewardRoot==null)
                {
                    var reward=SharedPresentationMigration.Add<TavernPanel>("Received item",parent,0,76,242,280);reward.color=new Color(.16f,.09f,.045f,.96f);reward.raycastTarget=false;v.rewardRoot=reward.gameObject;
                    v.rewardIcon=SharedPresentationMigration.Add<RawImage>("Item icon",reward.transform,0,22,168,204);v.rewardIcon.raycastTarget=false;
                    v.rewardLabel=SharedPresentationMigration.Label("Item label",reward.transform,0,-113,214,42,21);
                }
                v.rewardRoot.SetActive(false);
                foreach(var b in new[]{v.previous,v.menu,v.skip,v.next})b.transform.SetAsLastSibling();v.progress.transform.SetAsLastSibling();
                var controls=v.hub.transform.Find("Controls");if(controls!=null)controls.GetComponent<Text>().text="ЛКМ — показать текст / дальше · Двойной ЛКМ — следующая строка · Пробел / Enter — дальше";
                PrefabUtility.SaveAsPrefabAsset(g,CampaignAuthoring.Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
        }
    }
}
