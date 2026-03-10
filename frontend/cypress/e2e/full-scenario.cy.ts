describe('Scénario Complet & Sécurité', () => {
  
  // Cette étape s'exécute avant chaque test (Happy path et Cas limite)
  beforeEach(() => {
    // 1. UTILISATION DE LA COMMANDE (Remplace les 20 lignes de login manuel)
    cy.registerAndLogin(); 
    
    // On prépare l'écoute de l'upload pour tous les tests
    cy.intercept('POST', '**/api/**files*').as('uploadRequest');
  });

  it('Doit garantir l\'intégrité des données du début à la fin (Happy Path)', () => {
    // --- DONNÉES DE TEST ---
    const timestamp = Date.now();
    const fileName = 'secret-mission.txt';
    const fileContent = `Ce fichier contient des secrets générés à ${timestamp}`;

    // ============================================================
    // ÉTAPE 2 : UPLOAD DU FICHIER
    // ============================================================
    // Note: On est déjà connecté et sur la page d'accueil grâce au beforeEach
    
    cy.get('input[type="file"]').selectFile({
      contents: Cypress.Buffer.from(fileContent),
      fileName: fileName,
      mimeType: 'text/plain',
    }, { force: true });

    cy.get('button').contains(/téléverser|envoyer|upload|ajouter/i).click();

    cy.wait('@uploadRequest').then((interception) => {
      expect(interception.response?.statusCode).to.be.oneOf([200, 201]);
    });

    // ============================================================
    // ÉTAPE 3 : RÉCUPÉRATION DU LIEN
    // ============================================================
    cy.contains('http').then(($element) => {
        let shareUrl = $element.text() || $element.val();
        
        if (!shareUrl || !shareUrl.toString().includes('http')) {
             cy.get('@uploadRequest').then((interception) => {
                 const token = interception.response?.body.token;
                 shareUrl = `${Cypress.config().baseUrl}/download/${token}`;
                 cy.wrap(shareUrl).as('fileUrl');
             });
        } else {
            cy.wrap(shareUrl.toString()).as('fileUrl');
        }
    });

    // ============================================================
    // ÉTAPE 4 : SIMULATION D'UN AUTRE UTILISATEUR
    // ============================================================
    cy.clearAllLocalStorage();
    cy.clearAllSessionStorage();
    cy.clearCookies();
    
    cy.visit('/login');
    cy.contains(/se connecter|connexion/i).should('be.visible');

    // ============================================================
    // ÉTAPE 5 : TÉLÉCHARGEMENT & VÉRIFICATION
    // ============================================================
    cy.get('@uploadRequest').then((interception: any) => {
        const token = interception.response?.body.token || interception.response?.body.id;

        // J'ai gardé ta configuration exacte (POST et URL publique) puisque tu as confirmé qu'elle fonctionne
        cy.request({
            method: 'POST',
            url: `/api/public/files/${token}/download`,
            body: {},
        }).then((response) => {
            expect(response.status).to.eq(200);
            expect(response.body).to.equal(fileContent);
        });
    });
  });
});
