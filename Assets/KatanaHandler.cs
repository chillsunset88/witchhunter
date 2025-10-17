using UnityEngine;

/// <summary>
/// Menangani combo slash katana menggunakan parameter float Animator (default: "slash.").
/// Menekan Fire1 akan menaikkan nilai combo dari 1..maxComboStep, lalu reset ke 0 setelah jeda.
/// </summary>
public class KatanaHandler : MonoBehaviour
{
    [Header("References")]
    public Transform katana;          // drag 'sword'
    public Transform backSocket;      // drag KatanaBackSocket
    public Transform handSocket;      // drag KatanaHandSocket
    public Animator animator;         // drag Animator karakter

    [Header("Animator Combo Settings")]
    [Tooltip("Nama parameter float pada Animator yang mengontrol state combo.")]
    public string slashFloatName = "slash.";
    [Tooltip("Jumlah langkah combo maksimum (misal 3 untuk Slash1->Slash3).")]
    [Min(1)] public int maxComboStep = 3;
    [Tooltip("Waktu toleransi antar input sebelum combo di-reset (detik).")]
    public float comboResetDelay = 1f;

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

    int currentComboStep;
    float lastAttackTime;
    bool comboActive;

    void OnEnable()
    {
        ApplyCursorState(true);
    }

    void OnDisable()
    {
        ApplyCursorState(false);
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
            RegisterSlashInput();
        }

        if (comboActive && faceCursorWhileSlashing)
        {
            FaceTowardsCursor(false);
        }

        if (comboActive && Time.time - lastAttackTime >= comboResetDelay)
        {
            ResetCombo();
        }
    }

    void RegisterSlashInput()
    {
        if (animator == null) return;

        lastAttackTime = Time.time;
        currentComboStep = Mathf.Clamp(currentComboStep + 1, 1, maxComboStep);
        animator.SetFloat(slashFloatName, currentComboStep);

        if (!comboActive)
        {
            AttachToHand();
            comboActive = true;
            if (faceCursorWhileSlashing)
                FaceTowardsCursor(true);
        }
    }

    public void ResetCombo()
    {
        currentComboStep = 0;
        comboActive = false;
        if (animator != null)
            animator.SetFloat(slashFloatName, 0f);

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
        if (!comboActive)
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
