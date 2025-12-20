using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BattleState { Start, PlayerTurn, EnemyTurn, SelectingSkill, SelectingTarget, Win, Lose }

public class BattleSystem : MonoBehaviour
{
    public static BattleSystem Instance;

    [Header("References")]
    public GameObject heroPrefab;
    public BattleHUD battleHUD;
    public Transform[] heroSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("UI Helpers")]
    public GameObject selectionMarkerPrefab; 
    private GameObject currentMarker;       

    [Header("State")]
    public BattleState state;

    private List<BattleUnit> allUnits = new List<BattleUnit>();
    private int currentTurnIndex = 0;

    private int currentSkillIndex = 0;
    private List<RuntimeSkill> currentActiveSkills;

    // --- TARGETING VARIABLES ---
    private RuntimeSkill selectedSkill;
    private List<BattleUnit> validTargets = new List<BattleUnit>();
    private int currentTargetIndex = 0;

    public string ID_AI = "0xAI";
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    void Update()
    {
        if (state == BattleState.SelectingTarget)
        {
            HandleTargetSelectionInput();
        }
        else if (state == BattleState.SelectingSkill)
        {
            HandleSkillSelectionInput();
        }
    }
    void HandleTargetSelectionInput()
    {
        if (validTargets.Count == 0) return;

        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentTargetIndex++;
            if (currentTargetIndex >= validTargets.Count) currentTargetIndex = 0;
            hasInput = true;
        }

        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = validTargets.Count - 1;
            hasInput = true;
        }

        if (hasInput)
        {
            UpdateMarkerPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmTargetSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSelection();
        }
    }

    void ConfirmTargetSelection()
    {
        BattleUnit target = validTargets[currentTargetIndex];

        if (currentMarker) currentMarker.SetActive(false);

        StartCoroutine(ExecuteSkill_SingleTarget(allUnits[currentTurnIndex], target, selectedSkill));
    }

    void CancelSelection()
    {
        Debug.Log("Cancel target select");
        if (currentMarker) currentMarker.SetActive(false);

        state = BattleState.SelectingSkill;

        battleHUD.EnableActions(true);

        battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
    }
    void UpdateMarkerPosition()
    {
        BattleUnit target = validTargets[currentTargetIndex];

        if (currentMarker == null && selectionMarkerPrefab != null)
        {
            currentMarker = Instantiate(selectionMarkerPrefab);
        }

        if (currentMarker != null)
        {
            currentMarker.SetActive(true);

            Transform markerPos = target.transform.Find("VisualRoot/HeroUnitVisual/MarkerPosition");
            currentMarker.transform.position = markerPos.position;
        }
    }

    IEnumerator SetupBattle()
    {
        int currentTeamIndex = TeamManager.Instance.CurrentBattleTeamIndex;
        int index = 0;
        foreach(string id in TeamManager.Instance.GetHeroesInTeam(currentTeamIndex))
        {
            Hero hero = PlayerInventory.Instance.GetHeroById(id);
            if(hero != null)
            {
                SpawnUnit(PlayerProfile.Instance.WalletAddress, hero, heroSpawnPoints[index]);
                index++;
            }
            else
            {
                Debug.Log("Error: Can't find hero has id: " + id);
            }
        }

        SpawnUnit(ID_AI, new Hero("HR01", 20, "10"), enemySpawnPoints[0]);
        SpawnUnit(ID_AI, new Hero("HR02", 20, "10"), enemySpawnPoints[1]);

        allUnits = allUnits.OrderByDescending(hero => hero.speed).ToList();

        battleHUD.UpdateTurnOrderBar(allUnits, allUnits[0]);

        yield return new WaitForSeconds(1.0f);
        currentTurnIndex = 0;
        StartTurn();
    }

    void SpawnUnit(string ownerId, Hero hero, Transform spawnPoint)
    {
        GameObject go = Instantiate(heroPrefab, spawnPoint);
        BattleUnit unit = go.GetComponent<BattleUnit>();
        unit.SetupHero(hero, ownerId);
        allUnits.Add(unit);
    }

    void StartTurn()
    {
        if (allUnits.Count == 0) return;
        BattleUnit currentUnit = allUnits[0];

        if (currentUnit.currentHP <= 0 || currentUnit.IsDead)
        {
            MoveCurrentUnitToBack(); // Đá nó xuống dưới
            StartTurn(); // Gọi lại
            return;
        }

        currentUnit.OnTurnStart();
        battleHUD.UpdateHeroStatus(currentUnit);

        ReorderUnitsStartTurn(currentUnit);

        if (currentUnit.ownerId == PlayerProfile.Instance.WalletAddress)
        {
            state = BattleState.SelectingSkill; 
            currentSkillIndex = 0;             
            currentActiveSkills = currentUnit.ActiveSkills; 

            battleHUD.SetupSkillButtons(currentActiveSkills);
            battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
            battleHUD.EnableActions(true);
        }
        else
        {
            state = BattleState.EnemyTurn;
            battleHUD.EnableActions(false);
            StartCoroutine(EnemyTurnLogic(currentUnit));
        }
    }
    void HandleSkillSelectionInput()
    {
        if (currentActiveSkills == null || currentActiveSkills.Count == 0) return;

        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentSkillIndex++;
            if (currentSkillIndex >= currentActiveSkills.Count) currentSkillIndex = 0; 
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentSkillIndex--;
            if (currentSkillIndex < 0) currentSkillIndex = currentActiveSkills.Count - 1; 
            hasInput = true;
        }

        if (hasInput)
        {
            battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnSkillSelected(currentActiveSkills[currentSkillIndex]);
        }
    }
    public void OnSkillSelected(RuntimeSkill skill)
    {
        if (state != BattleState.SelectingSkill) return;

        selectedSkill = skill;
        Debug.Log($"Selected Skill: {skill.skillData.Name}");

        switch (skill.skillData.TargetType)
        {
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.Ally: 
                StartTargetSelectionMode(skill.skillData.TargetType);
                break;

            case SkillTargetType.AllEnemies:
                StartCoroutine(ExecuteSkill_AllEnemies(allUnits[0], selectedSkill));
                break;
        }
    }

    void StartTargetSelectionMode(SkillTargetType type)
    {
        state = BattleState.SelectingTarget;
        battleHUD.EnableActions(false);

        validTargets.Clear();
        string myId = PlayerProfile.Instance.WalletAddress;

        if (type == SkillTargetType.SingleEnemy)
        {
            validTargets = allUnits.Where(u => u.ownerId != myId && u.currentHP > 0).ToList();
        }
        else if (type == SkillTargetType.Ally)
        {
            validTargets = allUnits.Where(u => u.ownerId == myId && u.currentHP > 0).ToList();
        }
        validTargets = validTargets.OrderBy(u => u.transform.position.x).ToList();

        if (validTargets.Count > 0)
        {
            currentTargetIndex = 0; 
            UpdateMarkerPosition();
        }
        else
        {
            Debug.Log("No target");
            state = BattleState.PlayerTurn;
            battleHUD.EnableActions(true);
        }
    }
    IEnumerator ExecuteSkill_SingleTarget(BattleUnit attacker, BattleUnit target, RuntimeSkill skill)
    {
        state = BattleState.EnemyTurn;

        if (attacker.animator != null)
            attacker.animator.SetTrigger("Attack");

        Debug.Log($"{attacker.unitName} starts using {skill.skillData.Name}");

        yield return new WaitForSeconds(skill.skillData.castDelay);

        if (skill.skillData.vfxPrefab != null)
        {
            bool hasProjectileHit = false;

            Transform spawnPos = attacker.attackSpawnPoint != null ? attacker.attackSpawnPoint : attacker.transform;

            GameObject vfxObj = Instantiate(skill.skillData.vfxPrefab, spawnPos.position, Quaternion.identity);
            SkillProjectile projectile = vfxObj.GetComponent<SkillProjectile>();

            if (projectile != null)
            {
                projectile.Setup(target.transform, () =>
                {
                    hasProjectileHit = true;
                });
            }
            else
            {
                hasProjectileHit = true;
            }
            yield return new WaitUntil(() => hasProjectileHit);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }
        ApplyDamageLogic(attacker, target, skill);

        yield return new WaitForSeconds(1f); 
        NextTurn();
    }

    void ApplyDamageLogic(BattleUnit attacker, BattleUnit target, RuntimeSkill skill)
    {
        int dmg = CalculateDamage(attacker, skill.skillData.AttackMultiplier);

        if (skill.skillData.isSkillHeal)
        {
            target.Heal(dmg);
        }
        else
        {
            target.TakeDamage(dmg); 
        }
    }
    IEnumerator ExecuteSkill_AllEnemies(BattleUnit attacker, RuntimeSkill skill)
    {
        state = BattleState.EnemyTurn;
        skill.currentCooldown = skill.skillData.Cooldown;

        if (attacker.animator != null)
            attacker.animator.SetTrigger("Attack");

        Debug.Log($"{attacker.unitName} uses AOE: {skill.skillData.Name}");

        yield return new WaitForSeconds(skill.skillData.castDelay);

        string myId = PlayerProfile.Instance.WalletAddress;
        var enemies = allUnits.Where(u => u.ownerId != myId && u.currentHP > 0).ToList();

        int totalProjectilesFired = 0;
        int projectilesHitCount = 0;  

        Transform spawnPos = attacker.attackSpawnPoint != null ? attacker.attackSpawnPoint : attacker.transform;

        foreach (var enemy in enemies)
        {
            if (skill.skillData.vfxPrefab != null)
            {
                totalProjectilesFired++;

                GameObject vfxObj = Instantiate(skill.skillData.vfxPrefab, spawnPos.position, Quaternion.identity);
                SkillProjectile projectile = vfxObj.GetComponent<SkillProjectile>();

                if (projectile != null)
                {
                    projectile.Setup(enemy.transform, () =>
                    {
                        ApplyDamageLogic(attacker, enemy, skill);
                        projectilesHitCount++;
                    });
                }
                else
                {
                    ApplyDamageLogic(attacker, enemy, skill);
                    projectilesHitCount++;
                }
            }
            else
            {
                ApplyDamageLogic(attacker, enemy, skill);
            }
            yield return new WaitForSeconds(0.1f);
        }

        if (skill.skillData.vfxPrefab != null && totalProjectilesFired > 0)
        {
            yield return new WaitUntil(() => projectilesHitCount >= totalProjectilesFired);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1f);
        NextTurn();
    }

    IEnumerator EnemyTurnLogic(BattleUnit enemy)
    {
        Debug.Log($"Enemy {enemy.unitName} is thinking...");
        yield return new WaitForSeconds(0.5f);

        RuntimeSkill chosenSkill = null;

        foreach (var skill in enemy.ActiveSkills)
        {
            if (skill.currentCooldown <= 0)
            {
                chosenSkill = skill;
                break;
            }
        }

        if (chosenSkill == null)
        {
            Debug.Log("Enemy has no skill ready! Skipping turn.");
            yield return new WaitForSeconds(0.5f);
            NextTurn();
            yield break;
        }

        BattleUnit target = null;

        switch (chosenSkill.skillData.TargetType)
        {
            case SkillTargetType.SingleEnemy:
                target = allUnits.FirstOrDefault(u => u.ownerId != enemy.ownerId && u.currentHP > 0);

                if (target != null)
                {
                    StartCoroutine(ExecuteSkill_SingleTarget(enemy, target, chosenSkill));
                }
                else
                {
                    state = BattleState.Lose;
                    NextTurn();
                }
                break;

            case SkillTargetType.AllEnemies:
                bool hasTargets = allUnits.Any(u => u.ownerId != enemy.ownerId && u.currentHP > 0);

                if (hasTargets)
                {
                    StartCoroutine(ExecuteSkill_AllEnemies(enemy, chosenSkill));
                }
                else
                {
                    NextTurn();
                }
                break;

            case SkillTargetType.Ally:
            target = allUnits.FirstOrDefault(u => u.ownerId == enemy.ownerId && u.currentHP > 0);
            StartCoroutine(ExecuteSkill_SingleTarget(enemy, target, chosenSkill));
            break;
        }
    }
    void NextTurn()
    {
        if (CheckBattleOver()) return;

        MoveCurrentUnitToBack();

        StartTurn();
    }

    bool CheckBattleOver()
    {
        string myId = PlayerProfile.Instance.WalletAddress;

        bool enemiesAlive = allUnits.Any(u => u.ownerId != myId && u.currentHP > 0);
        bool playersAlive = allUnits.Any(u => u.ownerId == myId && u.currentHP > 0);

        if (!enemiesAlive)
        {
            state = BattleState.Win;
            Debug.Log("VICTORY!");
            return true;
        }

        if (!playersAlive)
        {
            state = BattleState.Lose;
            Debug.Log("GAME OVER");
            return true;
        }

        return false;
    }

    int CalculateDamage(BattleUnit attacker, float multiplier)
    {
        return Mathf.RoundToInt(attacker.damage * multiplier);
    }
    void MoveCurrentUnitToBack()
    {
        if (allUnits.Count == 0) return;

        BattleUnit finishedUnit = allUnits[0];
        allUnits.RemoveAt(0);

        var livingOthers = allUnits.Where(u => !u.IsDead).ToList();
        var deadOthers = allUnits.Where(u => u.IsDead).ToList();

        allUnits.Clear();

        if (finishedUnit.IsDead)
        {
            allUnits.AddRange(livingOthers);
            allUnits.AddRange(deadOthers);
            allUnits.Add(finishedUnit);
        }
        else
        {
            allUnits.AddRange(livingOthers);
            allUnits.Add(finishedUnit);
            allUnits.AddRange(deadOthers);
        }
    }

    public void ReorderUnitsStartTurn(BattleUnit currentUnit)
    {
        allUnits.Remove(currentUnit);

        var livingOthers = allUnits.Where(u => !u.IsDead).ToList();
        var deadOthers = allUnits.Where(u => u.IsDead).ToList();

        allUnits.Clear();

        allUnits.Add(currentUnit); 
        allUnits.AddRange(livingOthers);
        allUnits.AddRange(deadOthers);

        battleHUD.UpdateTurnOrderBar(allUnits, allUnits[0]);
    }

}