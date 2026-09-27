using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TargetArrowGraphic:MaskableGraphic
    {
        public float width=16,headLength=30,headWidth=19;
        public WorldArrowView arrowPrefab;WorldArrowView view;ArrowFeedback feedback;
        public void SelectTarget(){feedback?.Select();}
        public void Set(Vector2 a,Vector2 b,bool magic=false)
        {
            var board=FindFirstObjectByType<TableBoard>();if(board==null||board.ViewCamera==null)return;
            if(view==null){var prefab=arrowPrefab!=null?arrowPrefab:board.arrowPrefab;view=Instantiate(prefab,board.transform);view.name="Unified selection arrow";}
            view.gameObject.SetActive(true);view.SetScreen(board.ViewCamera,a,b,magic);
            if(feedback==null){feedback=GetComponent<ArrowFeedback>();if(feedback==null)feedback=gameObject.AddComponent<ArrowFeedback>();}feedback.Move(a,b);
        }
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();}
        protected override void OnDisable(){base.OnDisable();if(view!=null)view.gameObject.SetActive(false);}
        protected override void OnDestroy(){base.OnDestroy();if(view!=null)Destroy(view.gameObject);}
    }
}
