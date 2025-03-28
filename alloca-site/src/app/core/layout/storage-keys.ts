export const StorageKeys = {
    auth: 'user:auth',

    tema: 'user:tema',

    reserveUltima: 'reserve:ultima'
} as const;

export const StorageKeysLegacy = {
    auth: 'alloca.auth',
    reserveUltima: 'alloca.reserve.last'
} as const;
