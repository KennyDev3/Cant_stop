using UnityEngine;
using TMPro;
using System.Collections;
using StarterAssets;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialPhase
    {
        PickupSingle,
        DisposeSingle,
        AreaPickup,
        DisposeArea,
        ParryPractice,
        Combat,
        PickupEnemy,
        DisposeEnemy,
        TurretIntro,  
        CombatWave,    
        LootChest,     
        Finished       
    }

    [Header("Debug Settings")]
    [Tooltip("Select where the tutorial starts.")]
    [SerializeField] private TutorialPhase startingPhase = TutorialPhase.PickupSingle;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private GameObject stepCompleteVisual;
    [SerializeField] private TutorialFinishButton finishButton;

    [Header("Player Components")]
    [SerializeField] private PlayerGarbageHandler garbageHandler;
    [SerializeField] private PlayerAreaPickup areaPickupAbility;
    [SerializeField] private PlayerParryController parryAbility;
    [SerializeField] private ParryShield parryShield;
    [SerializeField] private PlayerStamina staminaAbility;
    [SerializeField] private TruckTurret turretScript;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Scene References")]
    [SerializeField] private GameObject singleGarbageItem;
    [SerializeField] private GameObject[] areaGarbageCluster;
    [SerializeField] private TutorialReceptacleListener trashCanListener;
    [SerializeField] private GameObject rangedEnemyObject;
    [SerializeField] private GameObject[] combatWaveEnemies;
    [SerializeField] private GameObject tutorialChest;

    // Internal State
    private bool _trashThrownInBin = false;
    private bool _enemyIsDead = false;
    private bool _singleEnemyIsDead = false;

    private int _waveEnemiesKilled = 0;
    private bool _itemWasLooted = false;


    private void Start()
    {
        if (areaPickupAbility) areaPickupAbility.enabled = false;
        if (parryAbility) parryAbility.enabled = false;
        if (parryShield) parryShield.enabled = false;
        if (turretScript) turretScript.enabled = false;

        if (singleGarbageItem) singleGarbageItem.SetActive(false);
        foreach (var item in areaGarbageCluster) item.SetActive(false);
        if (rangedEnemyObject) rangedEnemyObject.SetActive(false);
        if (tutorialChest) tutorialChest.SetActive(false);
        foreach (var enemy in combatWaveEnemies) enemy.SetActive(false);


        if (trashCanListener != null)
            trashCanListener.OnTrashThrownIn.AddListener(() => _trashThrownInBin = true);

        EnemyHealth.OnEnemyDeath += HandleEnemyDeath;

        if (inventoryManager != null)
            inventoryManager.OnItemPickedUp += HandleItemPickedUp;

        StartCoroutine(RunTutorialFlow());
    }

    private void OnDestroy()
    {
        // Clean up events
        EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;
        if (inventoryManager != null)
            inventoryManager.OnItemPickedUp -= HandleItemPickedUp;
    }


    private void HandleEnemyDeath(EnemyData data)
    {
        _singleEnemyIsDead = true;

        _waveEnemiesKilled++;
    }

    private void HandleItemPickedUp()
    {
        _itemWasLooted = true;
    }

    private IEnumerator RunTutorialFlow()
    {

        // ================= PHASE 1: SINGLE PICKUP =================
        if (startingPhase <= TutorialPhase.PickupSingle)
        {
            SetUI("Walk to the garbage. Press 'E' to Pick Up.");
            singleGarbageItem.SetActive(true);
            yield return new WaitUntil(() => garbageHandler.GetCurrentCapacity() > 0);
            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 2: DISPOSE SINGLE =================
        if (startingPhase <= TutorialPhase.DisposeSingle)
        {
            SetUI("Go to Disposal Area (White Square).\nPress 'R' to Throw (Hold R to throw farther).");
            if (startingPhase == TutorialPhase.DisposeSingle && garbageHandler.GetCurrentCapacity() == 0)
                singleGarbageItem.SetActive(true);

            _trashThrownInBin = false;
            yield return new WaitUntil(() => _trashThrownInBin == true);
            yield return new WaitForSeconds(0.5f);
            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 3: AREA PICKUP =================
        if (startingPhase <= TutorialPhase.AreaPickup)
        {
            SetUI("Press 'Q' to Pick Up multiple garbage items.");
            if (areaPickupAbility) areaPickupAbility.enabled = true;
            foreach (var item in areaGarbageCluster) item.SetActive(true);
            yield return new WaitUntil(() => garbageHandler.GetCurrentCapacity() >= 2);
            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 4: DISPOSE CLUSTER =================
        if (startingPhase <= TutorialPhase.DisposeArea)
        {
            SetUI("Return to Disposal Area.\nPress 'R' to Throw.");
            if (startingPhase == TutorialPhase.DisposeArea)
            {
                if (areaPickupAbility) areaPickupAbility.enabled = true;
                foreach (var item in areaGarbageCluster) item.SetActive(true);
            }
            _trashThrownInBin = false;
            yield return new WaitUntil(() => _trashThrownInBin == true);
            yield return new WaitForSeconds(0.5f);
            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 5: PARRY PRACTICE =================
        if (startingPhase <= TutorialPhase.ParryPractice)
        {
            SetUI("Combat Training Initializing.\nPress 'F' to Parry.");
            if (parryAbility) parryAbility.enabled = true;
            if (parryShield) parryShield.enabled = true;

            float practiceTimer = 4.0f;
            while (practiceTimer > 0)
            {
                SetUI($"Practice Parrying (F).\nNext Step in: {practiceTimer:F1}s");
                practiceTimer -= Time.deltaTime;
                yield return null;
            }
            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 6: COMBAT (ENEMY) =================
        if (startingPhase <= TutorialPhase.Combat)
        {
            SetUI("DEFEND YOURSELF!\nWait for the projectile, then Parry (F).");

            if (parryAbility) parryAbility.enabled = true;
            if (parryShield) parryShield.enabled = true;

            _enemyIsDead = false;

            if (rangedEnemyObject) rangedEnemyObject.SetActive(true);

            yield return new WaitUntil(() => _singleEnemyIsDead);

            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 7: PICKUP ENEMY =================
        if (startingPhase <= TutorialPhase.PickupEnemy)
        {
            EnableCombatAbilities();

            SetUI("Enemies turn to garbage on death.\nPress 'E' or 'Q' to pick up.");

            int currentAmount = garbageHandler.GetCurrentCapacity();

            yield return new WaitUntil(() => garbageHandler.GetCurrentCapacity() > currentAmount);

            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 8: DISPOSE ENEMY =================
        if (startingPhase <= TutorialPhase.DisposeEnemy)
        {
            EnableCombatAbilities();

            SetUI("Dispose of garbage in disposal area.\n(Press 'R' to Throw, Hold 'R' to throw Farther)");

            _trashThrownInBin = false;
            yield return new WaitUntil(() => _trashThrownInBin == true);
            yield return new WaitForSeconds(0.5f);

            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 9: TURRET INTRO =================
        if (startingPhase <= TutorialPhase.TurretIntro)
        {
            EnableCombatAbilities();

            SetUI("Turret Support Activated.\nYour Protector will now protect you.");

            if (turretScript) turretScript.enabled = true;

            yield return new WaitForSeconds(5.0f); // Wait 6 seconds

            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= PHASE 10: COMBAT WAVE =================
        if (startingPhase <= TutorialPhase.CombatWave)
        {
            SetUI("WARNING: Hostiles Detected.\nEliminate all targets.");

            // Ensure capabilities
            if (turretScript) turretScript.enabled = true;
            EnableCombatAbilities();

            _waveEnemiesKilled = 0;
            int targetKills = combatWaveEnemies.Length;

            foreach (var enemy in combatWaveEnemies) enemy.SetActive(true);

            yield return new WaitUntil(() => _waveEnemiesKilled >= targetKills);

            ShowSuccess();
            yield return new WaitForSeconds(1.5f);
        }

        // ================= PHASE 11: LOOT CHEST =================
        if (startingPhase <= TutorialPhase.LootChest)
        {
            SetUI("Area Secure. Items make you stronger.\nPress 'E' to Purchase the Chest.");

            // Reset flag
            _itemWasLooted = false;

            if (tutorialChest) tutorialChest.SetActive(true);

            // Wait for inventory event
            yield return new WaitUntil(() => _itemWasLooted);

            ShowSuccess();
            yield return new WaitForSeconds(1.0f);
        }

        // ================= FINISH =================
        if (finishButton != null)
        {
            SetUI("To Start the game, Press 'E' on the button.");

            finishButton.ActivateButton();
        }



    }

    private void EnableCombatAbilities()
    {
        if (parryAbility) parryAbility.enabled = true;
        if (parryShield) parryShield.enabled = true;
        if (turretScript) turretScript.enabled = true;
    }

    // --- UI HELPERS ---
    private void SetUI(string text)
    {
        if (stepCompleteVisual) stepCompleteVisual.SetActive(false);
        instructionText.text = text;
    }

    private void ShowSuccess()
    {
        if (stepCompleteVisual) stepCompleteVisual.SetActive(true);
    }
}