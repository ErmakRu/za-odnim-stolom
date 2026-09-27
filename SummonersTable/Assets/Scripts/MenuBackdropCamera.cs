using UnityEngine;
namespace SummonersTable
{
    [RequireComponent(typeof(Camera))]
    public sealed class MenuBackdropCamera : MonoBehaviour
    {
        public TableBoard board;
        Camera cameraComponent;
        void Awake(){cameraComponent=GetComponent<Camera>();cameraComponent.enabled=true;}
        void LateUpdate(){cameraComponent.enabled=board==null||!board.gameObject.activeInHierarchy;}
    }
}
