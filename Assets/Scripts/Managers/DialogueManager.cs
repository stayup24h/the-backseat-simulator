using UnityEngine;
using Yarn.Unity;

public class DialogueManager : MonoBehaviour
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

    public void TestDialogue()
    {
        dialogueRunner.StartDialogue("test");
    }
}
