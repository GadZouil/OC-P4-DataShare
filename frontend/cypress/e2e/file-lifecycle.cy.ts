describe('Gestion des fichiers (Upload & Delete)', () => {
  beforeEach(() => {
    cy.registerAndLogin();
  });

  it('Doit uploader un fichier depuis l\'accueil et le supprimer dans le dashboard', () => {
    // 1. Upload
    cy.visit('/');
    
    cy.get('input[type="file"]').selectFile({
      contents: Cypress.Buffer.from('Contenu du fichier test'),
      fileName: 'test-doc.txt',
      mimeType: 'text/plain',
    }, { force: true });
    
    cy.contains('button', /téléverser|envoyer|upload|ajouter|transférer/i).click();

    cy.contains(/http|télécharger|lien|success/i, { timeout: 10000 }).should('be.visible');

    // --- STABILISATION ---
    cy.wait(2000);

    // 2. Navigation
    cy.contains('nav a, aside a, header a', /Mon Espace|Mes fichiers/i).click();
    cy.url().should('include', '/me'); // Correction ici aussi

    // 3. Suppression
    // On cherche le fichier et on clique sur son bouton supprimer
    cy.contains('test-doc.txt', { timeout: 10000 })
      .parents('li, tr, div.ds-me-row')
      .first()
      .within(() => {
        // On clique sur le bouton qui ressemble à une suppression (texte ou classe danger)
        cy.get('button, a').filter(':contains("Supprimer"), .ds-me-action-danger').click();
      });

    // 4. Vérification disparition
    cy.contains('test-doc.txt').should('not.exist');
  });
});
