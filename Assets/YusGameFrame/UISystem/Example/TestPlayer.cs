using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        Init();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SavePosion();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            var options = UIPanelOpenOptions.Default;
            options.layer = UILayer.Popup;
            options.addToStack = true;
            options.isModal = true;
            options.closeOnBlockerClick = true;

            UIManager.Instance.OpenPanel<PlayerInfoPanel>("PlayerInfo", options);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance.CloseTopPanel();
        }

        rb.linearVelocity = new Vector2(10 * Input.GetAxis("Horizontal"), 10 * Input.GetAxis("Vertical"));
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

