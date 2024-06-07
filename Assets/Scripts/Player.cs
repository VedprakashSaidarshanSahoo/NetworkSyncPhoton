using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviourPunCallbacks
{
    [SerializeField] Rigidbody m_CharacterController;
    [SerializeField] float m_Speed;
    [SerializeField] int m_Score = 0;
    [SerializeField] int m_HighScore = 0;
    [SerializeField] GameObject m_WeaponPrefab;
    [SerializeField] Transform m_WeaponSpawnPoint;
    [SerializeField] float m_WeaponSpawnDelay = 5f;
    [SerializeField] TMP_Text m_PlayerName;

    private PhotonView photonView;
    private Camera m_PlayerCamera;

    private bool m_CanFire;
    private bool m_GameOver;

    private void Awake()
    {
        m_CanFire = true;
        m_GameOver = false;
        photonView = GetComponent<PhotonView>();
        m_PlayerCamera = transform.GetChild(0).GetComponent<Camera>();

        if (!photonView.IsMine)
        {
            m_PlayerCamera.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Disable cameras of other players
        DisableCameras();
        PlayerUpdate();
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            Movement();
            Action();

            if (transform.position.y < -2 && !m_GameOver)
            {
                m_GameOver = true;
            }
        }
    }

    private void Action()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkCallbacks.DebugLogRich("Fire...", "red", NetworkCallbacks.DebugFont(FontStyle.italic), NetworkCallbacks.DebugFont(FontStyle.bold));
            if (m_CanFire)
            {
                Shoot();
            }
        }
    }

    private void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        m_CharacterController.MovePosition(m_CharacterController.position + move * m_Speed * Time.deltaTime);
    }

    private void Shoot()
    {
        var _weapon = PhotonNetwork.Instantiate(m_WeaponPrefab.name, m_WeaponSpawnPoint.position, m_WeaponSpawnPoint.rotation);
        _weapon.GetComponent<Rigidbody>().velocity = Vector3.forward * 50f;
        Destroy(_weapon, 4f);
        StartCoroutine(CanFire());
    }

    private IEnumerator CanFire()
    {
        m_CanFire = false;
        yield return new WaitForSeconds(1f);
        m_CanFire = true;
    }

    private void DisableCameras()
    {
        foreach (GameObject _pl in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (!_pl.GetComponent<PhotonView>().IsMine)
            {
                _pl.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    public void PlayerUpdate()
    {
        photonView.RPC("PlayerName", RpcTarget.All);
    }

    [PunRPC]
    private void PlayerName()
    {
        m_PlayerName.text = PhotonNetwork.NickName;
    }

    public void TakeDamage(float damage)
    {
        m_Score -= (int)damage;
        NetworkCallbacks.DebugLogRich($"Player's Score: {m_Score}", "red", NetworkCallbacks.DebugFont(FontStyle.bold), NetworkCallbacks.DebugFont(FontStyle.italic));
    }

    private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(m_Score);
            stream.SendNext(m_HighScore);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            m_Score = (int)stream.ReceiveNext();
            m_HighScore = (int)stream.ReceiveNext();
        }
    }
}
