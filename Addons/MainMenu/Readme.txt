1 ) Open  NetworkManagerMMO.cs. 

Search :
uiPopup.Show("Disconnected.");

Modify to match:
if (!changingCharacters)
     uiPopup.Show("Disconnected.");
--------------------------------------

2 ) Open UILogin.cs

Search :
if (manager.state == NetworkState.Offline || manager.state == NetworkState.Handshake)

Modify to match:
if (!manager.changingCharacters)
     if (manager.state == NetworkState.Offline || manager.state == NetworkState.Handshake)