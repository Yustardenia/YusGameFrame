using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YusGameFrame.Localization;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            LocalizationManager.Instance.ChangeLanguage(Language.en_us);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            LocalizationManager.Instance.ChangeLanguage(Language.zh_cn);
        }
    }
}
