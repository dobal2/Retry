using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject tutorialPanel;
    
    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private bool pauseGame = true;
    
    [Header("Dialogue")]
    [TextArea(3, 10)]
    [SerializeField] private string[] dialogues;
    
    private int currentIndex = 0;
    private bool isTyping = false;
    private bool skipTyping = false;
    private Coroutine typingCoroutine;
    
    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        StartTutorial();
    }
    
    private void Update()
    {
        if (!tutorialPanel.activeInHierarchy) return;
        
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                skipTyping = true;
            }
            else
            {
                NextDialogue();
            }
        }
    }
    
    public void StartTutorial()
    {
        if (dialogues == null || dialogues.Length == 0) return;
        
        currentIndex = 0;
        tutorialPanel.SetActive(true);
        
        if (pauseGame)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        ShowCurrentDialogue();
    }
    
    private void ShowCurrentDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
    }
    
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        skipTyping = false;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            if (skipTyping)
            {
                dialogueText.text = text;
                break;
            }
            
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        
        isTyping = false;
    }
    
    private void NextDialogue()
    {
        currentIndex++;
        
        if (currentIndex >= dialogues.Length)
        {
            EndTutorial();
        }
        else
        {
            ShowCurrentDialogue();
        }
    }
    
    private void EndTutorial()
    {
        tutorialPanel.SetActive(false);
        
        if (pauseGame)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    public void SkipTutorial()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        EndTutorial();
    }
}