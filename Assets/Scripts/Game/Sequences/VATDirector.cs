using System.Collections.Generic;
using UnityEngine;

public class VATDirector : MonoBehaviour
{
    public List<VATAnimationData.VATAnimation> animations;

    void Start()
    {
        animations = new List<VATAnimationData.VATAnimation>();

        VATController[] controllers = GetComponentsInChildren<VATController>();
        foreach (VATController controller in controllers)
        {
            VATAnimationData animations = controller.animationData;
            foreach (VATAnimationData.VATAnimation animation in animations.animations)
            {
                this.animations.Add(animation);
            }
        }
    }

    
}
