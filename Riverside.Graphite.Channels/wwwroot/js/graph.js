const startup = () => {

    import { registerMgtComponents, registerMgtMsal2Provider } from "https://unpkg.com/@microsoft/mgt@4";
    registerMgtMsal2Provider();
    registerMgtComponents();

};

export default startup;