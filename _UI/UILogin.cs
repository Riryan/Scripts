using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UILogin : MonoBehaviour
{
    public UIPopup uiPopup;
    public NetworkManagerMMO manager;
    public NetworkAuthenticatorMMO auth;
    public GameObject panel;
    public Text statusText;
    public InputField accountInput;
    public InputField passwordInput;
    public Dropdown serverDropdown;
    public Button loginButton;
    public Button registerButton;
    [TextArea(1, 30)] public string registerMessage = "First time? Just log in and we will\ncreate an account automatically.";
    public Button hostButton;
    public Button dedicatedButton;
    public Button cancelButton;
    public Button quitButton;

    void Start()
    {
        // restore last selected server
        if (PlayerPrefs.HasKey("LastServer"))
        {
            string last = PlayerPrefs.GetString("LastServer", "");
            serverDropdown.value = manager.serverList.FindIndex(s => s.name == last);
        }

        // Host & Dedicated visibility:
        // - Visible in Editor (for quick testing)
        // - Hidden in non-Editor builds (player, server, etc.)
#if UNITY_EDITOR
        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (dedicatedButton != null) dedicatedButton.gameObject.SetActive(true);
#else
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (dedicatedButton != null) dedicatedButton.gameObject.SetActive(false);
#endif
    }

    void OnDestroy()
    {
        // remember last selected server name
        PlayerPrefs.SetString("LastServer", serverDropdown.captionText.text);
    }

    void Update()
    {
        // only show panel while offline or in handshake
        if (manager.state == NetworkState.Offline || manager.state == NetworkState.Handshake)
        {
            panel.SetActive(true);

            // status label
            if (NetworkClient.isConnecting)
                statusText.text = "Connecting...";
            else if (manager.state == NetworkState.Handshake)
                statusText.text = "Handshake...";
            else
                statusText.text = "";

            // register
            registerButton.interactable = !manager.isNetworkActive;
            registerButton.onClick.SetListener(() => { uiPopup.Show(registerMessage); });

            // login
            loginButton.interactable = !manager.isNetworkActive && auth.IsAllowedAccountName(accountInput.text);
            loginButton.onClick.SetListener(() => { manager.StartClient(); });

            // host (Editor only: compiled out in player builds)
#if UNITY_EDITOR
            if (hostButton != null)
            {
                hostButton.interactable =
                    Application.platform != RuntimePlatform.WebGLPlayer &&
                    !manager.isNetworkActive &&
                    auth.IsAllowedAccountName(accountInput.text);
                hostButton.onClick.SetListener(() => { manager.StartHost(); });
            }
#endif

            // cancel connect
            cancelButton.gameObject.SetActive(NetworkClient.isConnecting);
            cancelButton.onClick.SetListener(() => { manager.StopClient(); });

            // dedicated server (Editor only: compiled out in player builds)
#if UNITY_EDITOR
            if (dedicatedButton != null)
            {
                dedicatedButton.interactable =
                    Application.platform != RuntimePlatform.WebGLPlayer &&
                    !manager.isNetworkActive;
                dedicatedButton.onClick.SetListener(() => { manager.StartServer(); });
            }
#endif

            // quit
            quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });

            // pass login data to authenticator
            auth.loginAccount = accountInput.text;
            auth.loginPassword = passwordInput.text;

            // server list / address
            serverDropdown.interactable = !manager.isNetworkActive;
            serverDropdown.options = manager.serverList.Select(
                sv => new Dropdown.OptionData(sv.name)
            ).ToList();
            manager.networkAddress = manager.serverList[serverDropdown.value].ip;
        }
        else
        {
            panel.SetActive(false);
        }
    }
}
