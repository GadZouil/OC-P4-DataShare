// cypress/support/commands.ts

declare namespace Cypress {
  interface Chainable<Subject = any> {
    registerAndLogin(): Chainable<void>;
  }
}

Cypress.Commands.add('registerAndLogin', () => {
  const timestamp = Date.now();
  const email = `user${timestamp}@test.com`;
  const password = 'Password123!';

  // 1. DÉFINITION DES INTERCEPTS (Version Glob String + Wildcards)
  // On utilise des chaines '**' qui sont souvent plus stables que les Regex pour les problèmes de ports
  cy.intercept('POST', '**/auth/register').as('registerReq');
  cy.intercept('POST', '**/auth/login').as('loginReq');

  // 2. INSCRIPTION
  cy.visit('/register');
  cy.get('input[type="email"]').type(email);
  cy.get('input[type="password"]').first().type(password);
  
  // ASTUCE : On valide avec {enter} dans le dernier champ
  // Cela évite les problèmes de "clic qui rate" ou de bouton désactivé une milliseconde
  cy.get('input[type="password"]').last().type(`${password}{enter}`);

  // On garde le wait, mais on augmente légèrement le timeout au cas où le serveur (port 5180) est lent à répondre au premier appel
  cy.wait('@registerReq', { timeout: 10000 }).its('response.statusCode').should('eq', 200);

  // 3. CONNEXION
  // On attend une demi-seconde pour être sûr que la base de données a bien ingéré l'utilisateur
  cy.wait(500); 
  
  cy.visit('/login');
  cy.get('input[type="email"]').type(email);
  cy.get('input[type="password"]').type(`${password}{enter}`); // Idem, validation via Entrée

  cy.wait('@loginReq').its('response.statusCode').should('eq', 200);

  // Vérification finale
  cy.location('pathname').should('not.include', '/login');
});
