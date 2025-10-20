using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menangani combo slash katana menggunakan parameter float Animator (default: "slash.").
/// Input yang masuk akan dimasukkan ke queue dan dieksekusi berurutan mengikuti durasi tiap animasi,
/// sehingga perpindahan socket (hand/back) tetap sinkron dengan clip yang sedang dimainkan.
/// </summary>
public class KatanaHandler : MonoBehaviour
{
    public enum AttachmentAction
    {
        None,
        AttachToHand,
        AttachToBack
    }

    [System.Serializable]
    public class SlashStepDefinition
    {
        [Tooltip("Nama referensi opsional untuk step ini (hanya untuk memudahkan inspector).")]
        public string name = "Slash";
        [Min(1), Tooltip("Nilai float yang akan dikirim ke Animator untuk step ini.")]
        public int comboValue = 1;
        [Min(0f), Tooltip("Berapa lama (detik) nilai combo ditahan sebelum step berikutnya dijalankan.")]
        public float holdDuration = 0.7f;
        [Tooltip("Aksi socket ketika step dimulai.")]
        public AttachmentAction onEnterAttachment = AttachmentAction.AttachToHand;
        [Tooltip("Aksi socket ketika step selesai.")]
        public AttachmentAction onExitAttachment = AttachmentAction.None;
    }

    [Header("References")]
    public Transform katana;          // drag 'sword'
    public Transform backSocket;      // drag KatanaBackSocket
    public Transform handSocket;      // drag KatanaHandSocket
    public Animator animator;         // drag Animator karakter

    [Header("Animator Combo Settings")]
    [Tooltip("Nama parameter float pada Animator yang mengontrol state combo.")]
    public string slashFloatName = "slash.";
    [Tooltip("Langkah combo yang tersedia. Isi sesuai jumlah animasi (slash, slash1, slash2, dst).")]
    public SlashStepDefinition[] slashSteps = new SlashStepDefinition[3];
    [Tooltip("Waktu toleransi input baru setelah step terakhir sebelum combo dianggap selesai.")]
    public float comboResetDelay = 0.75f;

    [Header("Combo Finish Behaviour")]
    [Tooltip("Kembalikan katana ke punggung otomatis ketika combo benar-benar selesai.")]
    public bool returnToBackWhenFinished = true;

    [Header("Cursor Control")]
    [Tooltip("Lock cursor ke jendela game saat script aktif.")]
    public bool lockCursor = true;
    [Tooltip("Sembunyikan kursor saat dikunci.")]
    public bool hideCursorWhenLocked = true;

    [Header("Slash Facing")]
    [Tooltip("Saat combo aktif, karakter otomatis menghadap posisi kursor.")]
    public bool faceCursorWhileSlashing = true;
    [Tooltip("Kecepatan slerp rotasi saat mengikuti kursor.")]
    public float faceRotationSpeed = 12f;
    [Tooltip("Camera yang digunakan untuk melacak kursor. Kosongkan untuk pakai Camera.main.")]
    public Camera cursorCamera;
    [Tooltip("Normal bidang tempat kursor diproyeksikan (default: Vector3.up).")]
    public Vector3 planeNormal = Vector3.up;

    readonly Queue<SlashStepDefinition> slashQueue = new Queue<SlashStepDefinition>();

    Coroutine comboRoutine;
    int currentComboStep;
    int highestRequestedStep;
    bool comboActive;

    public bool IsComboActive => comboActive;

    void OnValidate()
    {
        if (slashSteps == null) return;

        for (int i = 0; i < slashSteps.Length; i++)
        {
            if (slashSteps[i] == null)
                slashSteps[i] = new SlashStepDefinition();

            if (slashSteps[i].comboValue <= 0)
                slashSteps[i].comboValue = i + 1;

            if (string.IsNullOrEmpty(slashSteps[i].name))
                slashSteps[i].name = $"Slash {i + 1}";
        }
    }

    void OnEnable()
    {
        ApplyCursorState(true);
    }

    void OnDisable()
    {
        ApplyCursorState(false);
        ResetCombo();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursorState(true);
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            QueueSlashInput();
        }

        if (comboActive && faceCursorWhileSlashing)
        {
            FaceTowardsCursor(false);
        }
    }

    void QueueSlashInput()
    {
        if (animator == null)
        {
            Debug.LogWarning("KatanaHandler: Animator belum diisi, combo tidak dapat diproses.", this);
            return;
        }

        if (slashSteps == null || slashSteps.Length == 0)
        {
            Debug.LogWarning("KatanaHandler: slashSteps kosong. Isi daftar step di inspector.", this);
            return;
        }

        int baseStep = Mathf.Max(currentComboStep, highestRequestedStep);
        int proposedStep = Mathf.Clamp(baseStep + 1, 1, slashSteps.Length);
        SlashStepDefinition definition = slashSteps[proposedStep - 1];
        slashQueue.Enqueue(definition);
        highestRequestedStep = proposedStep;

        bool startingCombo = comboRoutine == null;
        if (startingCombo)
        {
            comboActive = true;
            comboRoutine = StartCoroutine(ProcessSlashQueue());
            if (faceCursorWhileSlashing)
                FaceTowardsCursor(true);
        }
    }

    IEnumerator ProcessSlashQueue()
    {
        while (true)
        {
            if (slashQueue.Count == 0)
            {
                float waitTimer = 0f;
                while (slashQueue.Count == 0 && waitTimer < comboResetDelay)
                {
                    waitTimer += Time.deltaTime;
                    yield return null;
                }

                if (slashQueue.Count == 0)
                {
                    break;
                }
            }

            SlashStepDefinition step = slashQueue.Dequeue();
            currentComboStep = step.comboValue;
            highestRequestedStep = Mathf.Max(highestRequestedStep, currentComboStep);

            ExecuteAttachmentAction(step.onEnterAttachment);

            if (animator != null)
                animator.SetFloat(slashFloatName, currentComboStep);

            float duration = Mathf.Max(0f, step.holdDuration);
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            ExecuteAttachmentAction(step.onExitAttachment);
        }

        currentComboStep = 0;
        highestRequestedStep = 0;
        slashQueue.Clear();

        if (animator != null)
            animator.SetFloat(slashFloatName, 0f);

        comboRoutine = null;
        comboActive = false;

        if (returnToBackWhenFinished)
            AttachToBack();
    }

    public void ResetCombo()
    {
        if (comboRoutine != null)
        {
            StopCoroutine(comboRoutine);
            comboRoutine = null;
        }

        slashQueue.Clear();
        currentComboStep = 0;
        highestRequestedStep = 0;
        comboActive = false;

        if (animator != null)
            animator.SetFloat(slashFloatName, 0f);

        if (returnToBackWhenFinished)
            AttachToBack();
    }

    public void AttachToHand()
    {
        if (katana == null || handSocket == null) return;
        katana.SetParent(handSocket);
        katana.localPosition = Vector3.zero;
        katana.localRotation = Quaternion.identity;
    }

    public void AttachToBack()
    {
        if (katana == null || backSocket == null) return;
        katana.SetParent(backSocket);
        katana.localPosition = Vector3.zero;
        katana.localRotation = Quaternion.identity;
    }

    void ExecuteAttachmentAction(AttachmentAction action)
    {
        switch (action)
        {
            case AttachmentAction.AttachToHand:
                AttachToHand();
                break;
            case AttachmentAction.AttachToBack:
                AttachToBack();
                break;
        }
    }

    void ApplyCursorState(bool shouldLock)
    {
        if (!lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !(hideCursorWhenLocked && shouldLock);
    }

    void FaceTowardsCursor(bool instant)
    {
        if (!comboActive) return;
        Camera activeCamera = cursorCamera != null ? cursorCamera : Camera.main;
        if (activeCamera == null) return;
        if (planeNormal.sqrMagnitude < 0.0001f) return;

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(planeNormal.normalized, transform.position);
        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 direction = hitPoint - transform.position;
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        if (instant || faceRotationSpeed <= 0f)
        {
            transform.rotation = desiredRotation;
        }
        else
        {
            float t = 1f - Mathf.Exp(-faceRotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }
    }

    /// <summary>
    /// Opsional: panggil dari Animation Event pada akhir combo terakhir.
    /// Hanya akan melepas katana jika combo sudah reset.
    /// </summary>
    public void OnSlashAnimationComplete()
    {
        if (!comboActive && returnToBackWhenFinished)
        {
            AttachToBack();
        }
    }

    // void OnAnimatorMove()
    // {
    //     Vector3 newPosition = animator.rootPosition;
    //     newPosition.y = transform.position.y;  // jaga Y tetap di tanah
    //     transform.position = newPosition;
    //     transform.rotation = animator.rootRotation;
    // }
}
