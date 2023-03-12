#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEngine.Playables;

namespace Pegasus
{
    public class PegasusMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            //Get our inputs
            var inputCount = playable.GetInputCount();

            //Calculate blended progress
            float blendedProgress = 0;
            float totalWeight = 0;
            PegasusManager manager = null;
            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<PegasusBehaviour>)playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                if (manager == null) manager = input.pegasusManager;

                blendedProgress += input.pegasusProgress * inputWeight;
                totalWeight += inputWeight;
            }

            //We will only update if we got some weights i.e. we are being affected by the timeline
            if (!Mathf.Approximately(totalWeight, 0f))
                if (manager != null)
                    manager.MoveTargetTo(blendedProgress);
        }
    }
}
#endif