using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    private Rigidbody2D rb;
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SavePosion();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            UIManager.Instance.OpenPanel<PlayerInfoPanel>("PlayerInfo");
        }
        rb.velocity = new Vector2(10 *Input.GetAxis("Horizontal"), 10 *Input.GetAxis("Vertical"));
    }

    private void Init()
    {
        this.gameObject.transform.position = PlayerManager.Instance.CurrentPlayer.location;
    }

    private void SavePosion()
    {
        PlayerManager.Instance.ChangeLocation(this.gameObject.transform.position);
    }
}
