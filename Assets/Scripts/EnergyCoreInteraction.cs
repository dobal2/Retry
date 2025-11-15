using UnityEngine;

public class InteractableAnchorBox : TargetAnchorBox
{
    [Header("Interaction Settings")]
    [SerializeField] protected KeyCode interactionKey = KeyCode.F;
    [SerializeField] protected bool requireVisible = true;
    
    protected virtual void Update()
    {
        if (!CanInteract()) return;
        
        if (Input.GetKeyDown(interactionKey))
        {
            OnInteract();
        }
    }
    
    protected virtual bool CanInteract()
    {
        if (requireVisible && !IsAnchorBoxVisible())
            return false;
        
        return true;
    }
    
    protected virtual void OnInteract()
    {
        // 자식 클래스에서 구현
    }
    
    protected bool IsAnchorBoxVisible()
    {
        return anchorBoxUI != null && anchorBoxUI.gameObject.activeInHierarchy;
    }
}