export type BlockTargetType = 'Pavilion' | 'Room' | 'Desk';

export interface CreateBlockRequest {
    targetType: BlockTargetType;
    targetId: string;
    startUtc: string;
    endUtc: string;
    reason: string;
}

export interface CreateBlockResponse {
    id: string;
}

export interface BlockListItem {
    id: string;
    targetType: BlockTargetType;
    targetId: string;
    targetName: string;
    pavilionId: string | null;
    pavilionName: string | null;
    startUtc: string;
    endUtc: string;
    reason: string;
    isActive: boolean;
}

export interface ReasonRequest {
    reason: string;
}
