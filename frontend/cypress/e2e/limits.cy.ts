describe('Cas Limites et Sécurité', () => {
  beforeEach(() => {
    cy.registerAndLogin();
    cy.visit('/');
  });

  it('Doit gérer l\'envoi d\'un fichier vide (0 octet)', () => {
    // 1. On crée un fichier vide
    cy.get('input[type="file"]').selectFile({
      contents: Cypress.Buffer.from(''),
      fileName: 'empty.txt',
    }, { force: true });

    // 2. On prépare l'espion
    cy.intercept('POST', '/api/files').as('emptyUpload');

    // 3. On clique
    cy.contains('button', /téléverser|envoyer|upload|ajouter/i).click();

    // 4. Vérification
    // Soit le front empêche le clic (le bouton reste), soit le serveur renvoie une erreur (400)
    // On attend un peu pour voir la réaction
    cy.wait(1000);

    cy.get('body').then(($body) => {
        // Si une requête a été envoyée, on vérifie qu'elle a échoué
        // Sinon (si le front a bloqué), c'est bon aussi
        cy.get('@emptyUpload.all').then((interceptions) => {
            if (interceptions.length > 0) {
                // Si le serveur a répondu, ça doit être une erreur (400 Bad Request)
                expect(interceptions[0].response?.statusCode).to.not.equal(200);
                expect(interceptions[0].response?.statusCode).to.not.equal(201);
            } else {
                // Si pas de requête, c'est que le front a bloqué l'envoi (Bon comportement)
                cy.log('Le front-end a empêché l\'envoi du fichier vide');
            }
        });
    });
  });

  it('Doit gérer un fichier volumineux (5 Mo)', () => {
    // 1. Création fichier 5 Mo
    const bigFileContents = Cypress.Buffer.alloc(5 * 1024 * 1024);

    cy.get('input[type="file"]').selectFile({
      contents: bigFileContents,
      fileName: 'huge-file.bin',
    }, { force: true });

    // 2. IMPORTANT : On déclare l'espion AVANT le clic
    cy.intercept('POST', '/api/files').as('uploadReq');

    // 3. Clic
    cy.contains('button', /téléverser|envoyer|upload|ajouter/i).click();

    // 4. Attente de la réponse
    cy.wait('@uploadReq', { timeout: 30000 }).then((interception) => {
      // On accepte 201 (Succès) ou 413 (Trop gros)
      // Mais on refuse 500 (Erreur serveur interne / Crash)
      expect(interception.response?.statusCode).to.be.oneOf([200, 201, 413]);
    });
  });

  it('Doit refuser un fichier > 1 Go (Simulation Front-end)', () => {
    // Ruse : On injecte manuellement un fichier en mentant sur sa taille
    cy.get('input[type="file"]').then((subject) => {
      const file = new File([''], 'huge-movie.mp4', { type: 'video/mp4' });
      
      // On force la taille à 1.1 Go (1.1 * 1024^3)
      Object.defineProperty(file, 'size', { value: 1.1 * 1024 * 1024 * 1024 });

      // On simule l'ajout du fichier dans l'input
      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(file);
      const input = subject[0] as HTMLInputElement;
      input.files = dataTransfer.files;

      // On déclenche l'événement "change" pour que le site détecte le fichier
      cy.wrap(subject).trigger('change', { force: true });
    });

    // Action : Tenter d'envoyer
    cy.contains('button', /téléverser|envoyer|upload|ajouter/i).click();

    // Vérification :
    // 1. Soit un message d'erreur apparaît (ex: "Fichier trop volumineux")
    // 2. Soit le bouton reste inactif
    // 3. Mais surtout : on vérifie qu'on n'a PAS de succès immédiat
    cy.contains(/succès|lien/i).should('not.exist');
    
    // Optionnel : Si ton site affiche une erreur explicite, décommente la ligne suivante :
    // cy.contains(/trop lourd|volumineux|error|taille|limit/i).should('be.visible');
  });
});
