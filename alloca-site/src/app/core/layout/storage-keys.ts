/**
 * Chaves padronizadas para persistência no navegador (localStorage / sessionStorage).
 *
 * Convenção: separar segmentos com ":" para agrupar dados por domínio.
 * Ex.: "user:auth", "user:tema", "reserve:ultima".
 */
export const StorageKeys = {
    /** Sessão autenticada (token + dados do usuário). */
    auth: 'user:auth',
    /** Preferência de tema (claro/escuro). */
    tema: 'user:tema',
    /** Última seleção do fluxo de reserva. */
    reserveUltima: 'reserve:ultima'
} as const;

/**
 * Chaves antigas (sem ":") mantidas apenas para migração transparente.
 * Podem ser removidas após algum tempo em produção.
 */
export const StorageKeysLegacy = {
    auth: 'alloca.auth',
    reserveUltima: 'alloca.reserve.last'
} as const;
