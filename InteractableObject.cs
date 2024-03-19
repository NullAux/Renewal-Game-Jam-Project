using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    //Info for an in level object (eg bush) and a level exit. Only one class should be filled in in editor.
    enum InteractableType
    {
        InLevel,
        Exit
    }
    [SerializeField] InteractableType Type;
    [SerializeField] LevelObject LevelObjInfo;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenericInteract()//Called by PlayerControls.Interact
    {
        //Call appropriate interact for this object
        if (Type == InteractableType.InLevel)
        {
            //Call LevelObject.Interact
            LevelObjInfo.Interact();
            Debug.Log("Generic Interact");
        }

        else if (Type == InteractableType.Exit)
        {
            //Call appropriate
        }
        //Space for 3rd class of 'unique', with own identifying enum and swtich internal to the class.
    }

    [System.Serializable]
    class LevelObject
    {
        LevelObject(GameObject gameObject)
        {
            thisGameObject = gameObject;//Check this doesn't just create an (invisible) copy of the in scene object
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        }

        //For Editor
        [SerializeField] GameObject Item;
        [System.Serializable]
        public struct spriteSheet
        {
            [SerializeField] public Sprite springLoot;
            [SerializeField] public Sprite springEmpty;
            [SerializeField] public Sprite summerLoot;
            [SerializeField] public Sprite summerEmpty;
            [SerializeField] public Sprite fallLoot;
            [SerializeField] public Sprite fallEmpty;
        }
        [SerializeField] public spriteSheet SpriteSheet;


        //Internal
        bool hasResource = true;
        GameObject thisGameObject;
        SpriteRenderer spriteRenderer;

        public void Interact()
        {
            if (hasResource)
            {
                //Add resource to player's inventory
                hasResource = false;
            }
        }

        //Call when entering screen. GameManager provides saved info on state (ie season, if it's empty) used here.
        public void OnScreenLoad(GameManager.Seasons season, bool hasResource)
        {
            if (hasResource)
            {
                this.hasResource = true;

                //Set sprite (with resource)
                switch (season)
                {
                    case GameManager.Seasons.Spring:
                        spriteRenderer.sprite = SpriteSheet.springLoot;
                        break;
                    case GameManager.Seasons.Summer:
                        spriteRenderer.sprite = SpriteSheet.summerLoot;
                        break;
                    case GameManager.Seasons.Fall:
                        spriteRenderer.sprite = SpriteSheet.fallLoot;
                        break;
                }
                return;
            }

            //Set sprite (no resource)
            switch (season)
            {
                case GameManager.Seasons.Spring:
                    spriteRenderer.sprite = SpriteSheet.springEmpty;
                    break;
                case GameManager.Seasons.Summer:
                    spriteRenderer.sprite = SpriteSheet.summerEmpty;
                    break;
                case GameManager.Seasons.Fall:
                    spriteRenderer.sprite = SpriteSheet.fallEmpty;
                    break;
            }
            return;
        
    }

    }
}
