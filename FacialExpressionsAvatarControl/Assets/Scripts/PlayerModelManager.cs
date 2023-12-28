using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PlayerModelsManager : MonoBehaviour
{
    public GameObject playerModelsContainer;
    public Button incrementButton;
    public Button decrementButton;

    private List<GameObject> playerModels;
    private int currentIndex;

    private void Start()
    {
        // Get all immediate child objects of playerModelsContainer
        playerModels = new List<GameObject>();
        for (int i = 0; i < playerModelsContainer.transform.childCount; i++)
        {
            GameObject child = playerModelsContainer.transform.GetChild(i).gameObject;
            playerModels.Add(child);
        }

        // Set currentIndex to the last valid index
        currentIndex = playerModels.Count - 1;

        // Set up button listeners
        incrementButton.onClick.AddListener(IncrementModel);
        decrementButton.onClick.AddListener(DecrementModel);

        // Initially, activate the last model
        ActivateModel(currentIndex);

        // Disable decrement button if there's only one model
        decrementButton.interactable = playerModels.Count > 1;

        // Disable increment button if all models are visible
        incrementButton.interactable = !AllModelsVisible();
    }

    private void IncrementModel()
    {
        // Increment index to the nearest inactive model
        currentIndex++;
        // Activate the next model
        ActivateModel(currentIndex);

        // Update button interactability
        UpdateButtonInteractability();
    }

    private void DecrementModel()
    {
        if (currentIndex > 0)
        {
            DeactivateModel(currentIndex);

            currentIndex--;

            UpdateButtonInteractability();
        }
    }

    private void ActivateModel(int index)
    {
        playerModels[index].SetActive(true);
    }

    private void DeactivateModel(int index)
    {
        playerModels[index].SetActive(false);
    }

    private void UpdateButtonInteractability()
    {
        // Disable decrement button if there's only one model
        decrementButton.interactable = currentIndex > 0;

        // Disable increment button if all models are visible
        incrementButton.interactable = !AllModelsVisible();
    }

    private bool AllModelsVisible()
    {
        // Check if all models are currently visible
        return playerModels.All(model => model.activeSelf);
    }
}
