window.initializeGoogleAuth = (dotNetObjRef, clientId) => {
    google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => {
            dotNetObjRef.invokeMethodAsync('HandleGoogleLogin', response.credential);
        }
    });

    google.accounts.id.renderButton(
        document.getElementById("google-signin-btn"),
        { theme: "filled_black", size: "large", shape: "rectangular", text: "signin_with" }
    );
};
