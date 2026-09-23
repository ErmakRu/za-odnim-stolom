using UnityEngine;
namespace SummonersTable
{
    // The existing board uses eight scene units per metre. Keeping that conversion
    // preserves the authored card slots, targeting arrows and effect sizes.
    public sealed class TableProportions:MonoBehaviour
    {
        public float unitsPerMetre=8;
        public float humanReferenceHeight=1.8f;
        public float tableDiameter=1.5f,tableHeight=.75f,seatHeight=.46f;
        public float seatedCrownHeight=1.35f,hipHeight=.55f;
        public float playerRadius=1.06f,chairRadius=1.10f,chairWidth=.70f,chairDepth=.72f,chairBackHeight=1.12f;
        [Tooltip("Common rig scale: measured from the badger's hips to its crown. Animal ears and antlers retain their extra height.")]
        public float avatarScale;
    }
}
