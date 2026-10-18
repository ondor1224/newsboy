using System.Collections;
using System.Collections.Generic;
using UnityEngine;



    public enum BossAttackKind { GroundMinion, Bomb, StarProjectile, Soundwave, SideMinion, DiagonalMinion, BananaPeel }

    public class BossAttackObject : MonoBehaviour
    {
        public BossAttackKind kind;
        public bool isBossDirect;
        public float moveSpeed = 7f;
        public float lifeTime = 9f;
        public float soundwaveSafeJumpMinY = 1.1f;
        public float soundwaveSafeSlideMaxY = 1.0f;

        private Vector3 moveDir = Vector3.back, startPos, targetPos;
        private float duration = 0.8f, elapsed, riseTime = 0.55f, targetY, arcHeight = 3.8f;
        private bool isRising, isArc, hasHit;

        public void InitRising(float groundY, float rTime, float spd)
        {
            kind = BossAttackKind.GroundMinion; isBossDirect = false;
            targetY = groundY; riseTime = Mathf.Max(0.1f, rTime); moveSpeed = spd; isRising = true;
        }

        public void InitBomb(Vector3 orig, Vector3 dest, float fTime, float height)
        {
            kind = BossAttackKind.Bomb; isBossDirect = true;
            startPos = orig; targetPos = dest; duration = Mathf.Max(0.15f, fTime); arcHeight = height; isArc = true;
        }

        public void InitDir(BossAttackKind k, Vector3 dir, float spd, bool bossDirect)
        {
            kind = k; isBossDirect = bossDirect; moveDir = dir.normalized; moveSpeed = spd;
        }

        void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isPlaying) return;
            if ((elapsed += Time.deltaTime) >= lifeTime) { Destroy(gameObject); return; }

            if (isRising)
            {
                float t = Mathf.Clamp01(elapsed / riseTime);
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetY, t), transform.position.z);
                if (t >= 1f) isRising = false;
                return;
            }
            if (isArc)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(startPos, targetPos, t);
                p.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = p;
                if (t >= 1f) { isArc = false; moveDir = Vector3.back; }
                return;
            }
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Kill")) { Destroy(gameObject); return; }
            if (hasHit || isRising || GameManager.Instance == null || !GameManager.Instance.isPlaying) return;

            if (other.CompareTag("Player"))
            {
                player pl = other.GetComponent<player>();
                if (pl == null) return;
                if (kind == BossAttackKind.Soundwave)
                {
                    Bounds b = pl.ColliderBounds;
                    if ((pl.IsJumping && b.min.y >= soundwaveSafeJumpMinY) || (pl.IsSliding && b.max.y <= soundwaveSafeSlideMaxY)) return;
                }
                hasHit = true;
                if (isBossDirect) GameManager.Instance.DeductBossHit();
                else GameManager.Instance.DeductBasicHit();

                if (kind == BossAttackKind.Bomb || kind == BossAttackKind.StarProjectile || kind == BossAttackKind.BananaPeel)
                    Destroy(gameObject);
            }
        }
    }

    public class BossBattleManager : MonoBehaviour
    {
        public static BossBattleManager Instance { get; private set; }

        public AudioSource warningAudio;
        public AudioClip warningClip, bossAppearClip;
        public int triggerCoinPerChapter = 100;
        public int bossClearRewardCoin = 1000;

        public float ch1BossAppearTime = 180f, ch1Duration = 300f;
        public GameObject ch1BossObj;
        public BossAttackObject ch1MinionPrefab, ch1BombPrefab;

        public float ch2WaveUnlockTime = 60f, ch2Duration = 300f;
        public GameObject ch2BossObj;
        public BossAttackObject ch2StarPrefab, ch2WavePrefab;

        public float ch3DiagUnlockTime = 60f, ch3BossAppearTime = 150f, ch3Duration = 300f;
        public GameObject ch3BossObj;
        public BossAttackObject ch3SidePrefab, ch3DiagPrefab, ch3BananaPrefab;

        public GameObject groundWarnPrefab, laneWarnPrefab, fullWarnPrefab;
        public float[] laneX = { -2.5f, 0f, 2.5f };
        public float bossSpawnZ = 18f, attackTargetZ = 4f;

        public bool IsBossBattleActive { get; private set; }
        private bool ch1Started, ch2Started, ch3Started, ch1Done, ch2Done, ch3Done;
        private Coroutine masterCo;
        private readonly List<Coroutine> subCos = new List<Coroutine>();
        private readonly List<GameObject> spawned = new List<GameObject>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinUpdated += OnCoinUpdated;
                GameManager.Instance.OnGameEndedTriggered += AbortAll;
            }
            SetBossModels(null);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinUpdated -= OnCoinUpdated;
                GameManager.Instance.OnGameEndedTriggered -= AbortAll;
            }
        }

        void OnCoinUpdated(int total, int progress)
        {
            if (IsBossBattleActive || GameManager.Instance == null || !GameManager.Instance.isPlaying) return;
            if (progress < triggerCoinPerChapter) return;

            if (!ch1Started) StartBoss(1);
            else if (ch1Done && !ch2Started) StartBoss(2);
            else if (ch2Done && !ch3Started) StartBoss(3);
        }

        void StartBoss(int ch)
        {
            IsBossBattleActive = true;
            if (ch == 1) ch1Started = true;
            if (ch == 2) ch2Started = true;
            if (ch == 3) ch3Started = true;

            CleanupObjects();
            GameManager.Instance.SetCustomerSpawnEnabled(false, true);
            GameManager.Instance.SetBossBattleDifficulty(ch);

            if (masterCo != null) StopCoroutine(masterCo);
            masterCo = StartCoroutine(ch == 1 ? Ch1Loop() : (ch == 2 ? Ch2Loop() : Ch3Loop()));
        }

        IEnumerator Ch1Loop()
        {
            float t = 0f, mTimer = 2f, bTimer = 2.5f;
            bool appeared = false;
            while (t < ch1Duration)
            {
                if (!GameManager.Instance.isPlaying) yield break;
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / ch1Duration);

                if (!appeared && t >= ch1BossAppearTime)
                {
                    appeared = true;
                    ShowBoss(ch1BossObj);
                    yield return new WaitForSeconds(2f);
                }
                if ((mTimer -= Time.deltaTime) <= 0f)
                {
                    RunSub(SpawnCh1Minion((t > 120f && Random.value > 0.5f) ? 2 : 1, 1.5f));
                    mTimer = Mathf.Lerp(4.0f, 2.7f, p);
                }
                if (appeared && (bTimer -= Time.deltaTime) <= 0f)
                {
                    RunSub(SpawnCh1Bomb(1.5f));
                    bTimer = Mathf.Lerp(3.5f, 2.2f, p);
                }
                yield return null;
            }
            FinishChapter(1);
        }

        IEnumerator Ch2Loop()
        {
            ShowBoss(ch2BossObj);
            yield return new WaitForSeconds(2f);
            float t = 0f, cd = 1.8f;
            while (t < ch2Duration)
            {
                if (!GameManager.Instance.isPlaying) yield break;
                t += Time.deltaTime;
                if ((cd -= Time.deltaTime) <= 0f)
                {
                    float p = Mathf.Clamp01(t / ch2Duration);
                    if (t >= ch2WaveUnlockTime && Random.value < 0.42f)
                    {
                        RunSub(SpawnCh2Wave(Mathf.Lerp(1.5f, 1.2f, p), Mathf.Lerp(9f, 12f, p)));
                        cd = Mathf.Lerp(3.8f, 2.9f, p);
                    }
                    else
                    {
                        RunSub(SpawnCh2Star(p > 0.45f && Random.value < 0.5f ? 2 : 1, Mathf.Lerp(1.4f, 1.1f, p), Mathf.Lerp(10f, 13.5f, p)));
                        cd = Mathf.Lerp(3.0f, 2.0f, p);
                    }
                }
                yield return null;
            }
            FinishChapter(2);
        }

        IEnumerator Ch3Loop()
        {
            float t = 0f, cd = 1.6f;
            bool appeared = false;
            while (t < ch3Duration)
            {
                if (!GameManager.Instance.isPlaying) yield break;
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / ch3Duration);

                if (!appeared && t >= ch3BossAppearTime)
                {
                    appeared = true;
                    ShowBoss(ch3BossObj);
                    yield return new WaitForSeconds(2f);
                }
                if ((cd -= Time.deltaTime) <= 0f)
                {
                    if (appeared && Random.value < 0.45f)
                    {
                        RunSub(SpawnCh3Banana(Random.Range(1, Mathf.Min(3, laneX.Length)), Mathf.Lerp(1.3f, 1.0f, p)));
                        cd = Mathf.Lerp(2.7f, 1.85f, p);
                    }
                    else if (t >= ch3DiagUnlockTime && Random.value < 0.5f)
                    {
                        RunSub(SpawnCh3Diag(Mathf.Lerp(1.35f, 1.05f, p), Mathf.Lerp(10.5f, 13.5f, p)));
                        cd = Mathf.Lerp(2.5f, 1.7f, p);
                    }
                    else
                    {
                        RunSub(SpawnCh3Side(p > 0.5f && Random.value < 0.45f ? 2 : 1, Mathf.Lerp(1.3f, 1.0f, p), Mathf.Lerp(9.5f, 12.5f, p)));
                        cd = Mathf.Lerp(2.4f, 1.6f, p);
                    }
                }
                yield return null;
            }
            FinishChapter(3);
        }

        IEnumerator SpawnCh1Minion(int count, float warnTime)
        {
            if (!ch1MinionPrefab) yield break;
            List<int> lanes = PickLanes(count, false);
            List<GameObject> warns = CreateWarnings(lanes, groundWarnPrefab, 0.05f, bossSpawnZ - 4f);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            foreach (int l in lanes)
            {
                BossAttackObject m = Instantiate(ch1MinionPrefab, new Vector3(laneX[l], -2.5f, bossSpawnZ - 4f), Quaternion.identity);
                m.InitRising(0f, 0.5f, 6f);
                spawned.Add(m.gameObject);
            }
            yield return new WaitForSeconds(1f);
            FreeLanes(lanes);
        }

        IEnumerator SpawnCh1Bomb(float warnTime)
        {
            if (!ch1BombPrefab) yield break;
            List<int> lanes = PickLanes(1, true);
            List<GameObject> warns = CreateWarnings(lanes, groundWarnPrefab, 0.05f, attackTargetZ);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            Vector3 orig = ch1BossObj ? ch1BossObj.transform.position : new Vector3(0f, 2f, bossSpawnZ);
            Vector3 dest = new Vector3(laneX[lanes[0]], 0.05f, attackTargetZ);
            BossAttackObject b = Instantiate(ch1BombPrefab, orig, Quaternion.identity);
            b.InitBomb(orig, dest, 0.75f, 3.8f);
            spawned.Add(b.gameObject);
            yield return new WaitForSeconds(1f);
            FreeLanes(lanes);
        }

        IEnumerator SpawnCh2Star(int count, float warnTime, float spd)
        {
            if (!ch2StarPrefab) yield break;
            List<int> lanes = PickLanes(count, true);
            List<GameObject> warns = CreateWarnings(lanes, laneWarnPrefab, 0.8f, bossSpawnZ - 2f);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            foreach (int l in lanes)
            {
                BossAttackObject s = Instantiate(ch2StarPrefab, new Vector3(laneX[l], 1f, bossSpawnZ), Quaternion.identity);
                s.InitDir(BossAttackKind.StarProjectile, Vector3.back, spd, true);
                spawned.Add(s.gameObject);
            }
            yield return new WaitForSeconds(1f);
            FreeLanes(lanes);
        }

        IEnumerator SpawnCh2Wave(float warnTime, float spd)
        {
            if (!ch2WavePrefab) yield break;
            GameManager.Instance.SetObstacleSpawnPaused(true);
            PlayWarn();
            GameObject w = fullWarnPrefab ? Instantiate(fullWarnPrefab, new Vector3(0f, 1f, bossSpawnZ - 3f), Quaternion.identity) : null;
            if (w) spawned.Add(w);
            yield return new WaitForSeconds(warnTime);
            if (w) Destroy(w);
            if (!IsBossBattleActive) { GameManager.Instance.SetObstacleSpawnPaused(false); yield break; }

            BossAttackObject wave = Instantiate(ch2WavePrefab, new Vector3(0f, 1.05f, bossSpawnZ), Quaternion.identity);
            wave.InitDir(BossAttackKind.Soundwave, Vector3.back, spd, true);
            spawned.Add(wave.gameObject);
            yield return new WaitForSeconds(1.3f);
            GameManager.Instance.SetObstacleSpawnPaused(false);
        }

        IEnumerator SpawnCh3Side(int count, float warnTime, float spd)
        {
            if (!ch3SidePrefab) yield break;
            List<int> lanes = PickLanes(count, false);
            List<GameObject> warns = CreateWarnings(lanes, laneWarnPrefab, 0.5f, bossSpawnZ - 5f);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            foreach (int l in lanes)
            {
                Vector3 st = new Vector3(laneX[l] < 0f ? -6.5f : 6.5f, 0f, bossSpawnZ - 5f);
                Vector3 dir = (new Vector3(laneX[l], 0f, 0f) - st).normalized;
                BossAttackObject m = Instantiate(ch3SidePrefab, st, Quaternion.identity);
                m.InitDir(BossAttackKind.SideMinion, dir, spd, false);
                spawned.Add(m.gameObject);
            }
            yield return new WaitForSeconds(1f);
            FreeLanes(lanes);
        }

        IEnumerator SpawnCh3Diag(float warnTime, float spd)
        {
            if (!ch3DiagPrefab) yield break;
            List<int> lanes = PickLanes(1, true);
            List<GameObject> warns = CreateWarnings(lanes, groundWarnPrefab, 0.1f, attackTargetZ);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            Vector3 sky = new Vector3(Random.value < 0.5f ? -5.5f : 5.5f, 7.5f, bossSpawnZ);
            Vector3 dest = new Vector3(laneX[lanes[0]], 0.1f, attackTargetZ);
            BossAttackObject m = Instantiate(ch3DiagPrefab, sky, Quaternion.identity);
            m.InitDir(BossAttackKind.DiagonalMinion, (dest - sky).normalized, spd, false);
            spawned.Add(m.gameObject);
            yield return new WaitForSeconds(1f);
            FreeLanes(lanes);
        }

        IEnumerator SpawnCh3Banana(int count, float warnTime)
        {
            if (!ch3BananaPrefab) yield break;
            List<int> lanes = PickLanes(count, false);
            List<GameObject> warns = CreateWarnings(lanes, groundWarnPrefab, 0.05f, bossSpawnZ - 3f);
            yield return new WaitForSeconds(warnTime);
            ClearWarnings(warns);
            if (!IsBossBattleActive) { FreeLanes(lanes); yield break; }

            foreach (int l in lanes)
            {
                BossAttackObject b = Instantiate(ch3BananaPrefab, new Vector3(laneX[l], 0.1f, bossSpawnZ - 3f), Quaternion.identity);
                b.InitDir(BossAttackKind.BananaPeel, Vector3.back, 6.8f, true);
                spawned.Add(b.gameObject);
            }
            yield return new WaitForSeconds(1.1f);
            FreeLanes(lanes);
        }

        void FinishChapter(int ch)
        {
            if (!IsBossBattleActive || GameManager.Instance == null || GameManager.Instance.isGameEnded) return;
            StopAllSub();
            CleanupObjects();
            SetBossModels(null);
            IsBossBattleActive = false;

            GameManager.Instance.ClearAllReservedLanes();
            GameManager.Instance.SetObstacleSpawnPaused(false);
            GameManager.Instance.SetBossBattleDifficulty(0);

            if (ch == 1 && !ch1Done) { ch1Done = true; GameManager.Instance.AddBossClearReward(bossClearRewardCoin); GameManager.Instance.SetCustomerSpawnEnabled(true, false); }
            else if (ch == 2 && !ch2Done) { ch2Done = true; GameManager.Instance.AddBossClearReward(bossClearRewardCoin); GameManager.Instance.SetCustomerSpawnEnabled(true, false); }
            else if (ch == 3 && !ch3Done) { ch3Done = true; GameManager.Instance.AddBossClearReward(bossClearRewardCoin); GameManager.Instance.CompleteAllBossChapters(); }
        }

        void AbortAll(bool cleared, string quotaMsg, int rew, int settled, int totalSaved)
        {
            IsBossBattleActive = false;
            if (masterCo != null) StopCoroutine(masterCo);
            StopAllSub();
            CleanupObjects();
            SetBossModels(null);
        }

        void RunSub(IEnumerator r) => subCos.Add(StartCoroutine(r));
        void StopAllSub() { foreach (var c in subCos) if (c != null) StopCoroutine(c); subCos.Clear(); }

        List<GameObject> CreateWarnings(List<int> lanes, GameObject prefab, float y, float z)
        {
            PlayWarn();
            List<GameObject> list = new List<GameObject>();
            foreach (int l in lanes)
            {
                GameManager.Instance.ReserveLane(l);
                if (prefab) { GameObject w = Instantiate(prefab, new Vector3(laneX[l], y, z), Quaternion.identity); spawned.Add(w); list.Add(w); }
            }
            return list;
        }

        void ClearWarnings(List<GameObject> list) { foreach (var w in list) if (w) Destroy(w); }
        void FreeLanes(List<int> lanes) { foreach (int l in lanes) GameManager.Instance.ReleaseLane(l); }
        void CleanupObjects() { foreach (var o in spawned) if (o) Destroy(o); spawned.Clear(); }

        List<int> PickLanes(int count, bool biasPlayer)
        {
            int max = Mathf.Max(1, laneX.Length - 1);
            int target = Mathf.Clamp(count, 1, max);
            List<int> pool = new List<int>();
            for (int i = 0; i < laneX.Length; i++) pool.Add(i);
            List<int> res = new List<int>();
            for (int i = 0; i < target && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                res.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            if (biasPlayer && GameManager.Instance.playerRef != null)
            {
                int closest = 0;
                float minD = Mathf.Abs(GameManager.Instance.playerRef.transform.position.x - laneX[0]);
                for (int i = 1; i < laneX.Length; i++)
                {
                    float d = Mathf.Abs(GameManager.Instance.playerRef.transform.position.x - laneX[i]);
                    if (d < minD) { minD = d; closest = i; }
                }
                if (!res.Contains(closest)) res[0] = closest;
            }
            return res;
        }

        void PlayWarn() { if (warningAudio && warningClip) warningAudio.PlayOneShot(warningClip); }
        void ShowBoss(GameObject activeObj)
        {
            SetBossModels(activeObj);
            if (warningAudio && bossAppearClip) warningAudio.PlayOneShot(bossAppearClip);
        }
        void SetBossModels(GameObject activeObj)
        {
            if (ch1BossObj) ch1BossObj.SetActive(ch1BossObj == activeObj);
            if (ch2BossObj) ch2BossObj.SetActive(ch2BossObj == activeObj);
            if (ch3BossObj) ch3BossObj.SetActive(ch3BossObj == activeObj);
        }
    }
