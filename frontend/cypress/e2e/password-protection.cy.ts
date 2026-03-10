// cypress/e2e/password-protection.cy.ts

describe('Scénario Sécurité : Fichiers protégés par mot de passe', () => {
  
  beforeEach(() => {
    // On utilise notre nouvelle commande magique !
    cy.registerAndLogin();
  });

  it('Doit bloquer le téléchargement sans mot de passe et l\'autoriser avec', () => {
    const filePassword = 'SecretPassword123!';
    
    // 1. Préparer l'upload
    cy.visit('/'); // On est déjà connecté
    cy.get('input[type="file"]').selectFile(
      { contents: Cypress.Buffer.from('TOP SECRET DATA'), fileName: 'secret.txt', mimeType: 'text/plain' },
      { force: true }
    );

    // 2. Remplir le formulaire AVEC mot de passe
    // J'utilise le placeholder "Optionnel" que j'ai vu dans ton UploadView.vue
    cy.get('input[type="password"][placeholder="Optionnel"]')
      .type(filePassword);

    // 3. Envoyer
    cy.intercept('POST', '**/api/files').as('uploadRequest');
    cy.contains(/téléverser|envoyer/i).click();

    // 4. Récupérer l'ID du fichier
    cy.wait('@uploadRequest').then((interception) => {
      expect(interception.response?.statusCode).to.be.oneOf([200, 201]);
      // On gère les deux formats possibles de réponse (id ou token)
      const fileId = interception.response?.body.token || interception.response?.body.id;
      
      // ==========================================================
      // TEST DE SÉCURITÉ 1 : Tenter de télécharger SANS mot de passe
      // ==========================================================
      cy.request({
        method: 'POST',
        url: `/api/public/files/${fileId}/download`,
        body: {}, // Pas de mot de passe envoyé
        failOnStatusCode: false // Important : on s'attend à une erreur, donc ne pas faire échouer le test
      }).then((response) => {
        // On s'attend à une erreur 401 (Unauthorized) ou 403 (Forbidden)
        // Si tu reçois 200 ici, c'est une GRAVE faille de sécurité
        expect(response.status).to.be.oneOf([401, 403]); 
      });

      // ==========================================================
      // TEST DE SÉCURITÉ 2 : Tenter de télécharger AVEC le bon mot de passe
      // ==========================================================
      cy.request({
        method: 'POST',
        url: `/api/public/files/${fileId}/download`,
        body: { password: filePassword }, // On envoie le JSON avec le pass
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.eq('TOP SECRET DATA');
      });
    });
  });
});
