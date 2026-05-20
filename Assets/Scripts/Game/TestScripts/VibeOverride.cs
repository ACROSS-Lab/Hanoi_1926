using System.Collections.Generic;
using UnityEngine;

public class VibeOverride : MonoBehaviour
{
    [SerializeField] List <RuntimeAnimatorController> controllers;
    [SerializeField] GameObject characters;

    List<Animator> animators = new List<Animator>();
    Dictionary<Animator, RuntimeAnimatorController> originalControllers = new Dictionary<Animator, RuntimeAnimatorController>();

    void Start()
    {
        foreach (Animator character in characters.GetComponentsInChildren<Animator>())
        {
            originalControllers.Add(character, character.runtimeAnimatorController);
            animators.Add(character);
        }
    }

    public void StartVibing()
    {
        for (int i = 0; i < animators.Count; i++)
        {
            int random = Random.Range(0, controllers.Count);
            animators[i].runtimeAnimatorController = controllers[random];
        }
    }

    public void StopVibing()
    {
        for (int i = 0; i < animators.Count; i++)
        {
            animators[i].runtimeAnimatorController = originalControllers[animators[i]];
        }
    }
}