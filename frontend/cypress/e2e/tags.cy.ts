describe('Gestion des Tags', () => {
  beforeEach(() => {
    cy.registerAndLogin();
    cy.visit('/');
  });

  it('Doit ajouter un tag lors de l\'upload et le retrouver dans la liste', () => {
    // 1. Upload
    cy.get('input[type="file"]').selectFile({
      contents: Cypress.Buffer.from('Fichier tagué'),
      fileName: 'tagged-doc.txt',
    }, { force: true });

    cy.get('input[placeholder*="tag"], input[name="tags"]')
      .type('Important{enter}'); 

    cy.contains('button', /téléverser|envoyer|upload|ajouter/i).click();

    cy.contains(/http|télécharger|lien|success/i, { timeout: 10000 }).should('be.visible');

    // --- STABILISATION ---
    cy.wait(2000);

    // 2. Navigation vers "Mon Espace"
    // On clique sur le lien du menu (selon ton texte)
    cy.contains('nav a, aside a, header a', /Mon Espace|Mes fichiers/i).click();

    // CORRECTION ICI : On vérifie qu'on est sur /me
    cy.url().should('include', '/me');

    // 3. Vérification du fichier et du tag
    // On cherche "tagged-doc.txt" n'importe où dans la page principale
    cy.contains('tagged-doc.txt', { timeout: 10000 })
      .should('be.visible')
      .parents('li, tr, div.ds-me-row') // On remonte au parent (la ligne ou la carte)
      .first()
      .within(() => {
        // On vérifie que le tag "Important" est bien DANS cet élément
        cy.contains('Important').should('be.visible');
      });
  });
});
