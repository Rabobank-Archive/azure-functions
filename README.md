## Shared Secret 

Het authorization header dat meekomt vanuit de compliancy frontend in Azure DevOps is signed met het secret van de 
extension en daarop wordt gecontroleerd in de `Tokenizer`. Dit secret wordt meegegeven als secret variable in de pipeline
bij de deployment van de functions.

Mocht je het secret ooit opnieuw nodig hebben, dan kan je deze downloaden in de https://marketplace.visualstudio.com:

![certificate](docs/marketplace.png)