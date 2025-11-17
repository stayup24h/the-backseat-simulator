using UnityEngine;
using Yarn.Unity;

public class DialogueManager : SingletonBehaviour<DialogueManager>
{
    [SerializeField]
    private DialogueRunner dialogueRunner;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartDialogue(string dialogueNode)
    {
        GameManager.Instance.DecreaseOnInteract();
        dialogueRunner.StartDialogue(dialogueNode);
    }
}
