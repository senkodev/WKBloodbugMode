using System;

namespace BloodbugMode
{
    // https://docs.unity3d.com/ScriptReference/SerializeReference.html
    [Serializable]
    public class BloodbugModule : PerkModule
    {
        [NonSerialized]
        private BloodbugController controller;

        public override void Initialize(Perk p, bool firstTime)
        {
            base.Initialize(p, firstTime);
            controller = ENT_Player.GetPlayer().gameObject.AddComponent<BloodbugController>();
        }

        public override void OnDestroy(Perk p)
        {
            if (controller != null)
            {
                controller.Leave();
            }
        }
    }
}
