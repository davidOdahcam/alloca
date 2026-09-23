export interface OperatingHours {
    dayOfWeek: number;
    opensAt: string;
    closesAt: string;
}

export interface Pavilion {
    id: string;
    code: string;
    name: string;
    slotMinutes: number;
    minAdvanceMinutes: number;
    maxAdvanceDays: number;
    operatingHours: OperatingHours[];
}

export interface Floor {
    id: string;
    code: string;
    name: string;
    level: number;
    svgKey: string | null;
}

export type ResourceType = 'Room' | 'Desk';

export interface AvailabilityResource {
    id: string;
    externalId: string;
    name: string;
    type: ResourceType;
    roomId: string | null;
    available: boolean;
}

export interface CheckAvailabilityRequest {
    startUtc: string;
    endUtc: string;
}

export interface FloorDeskResource {
    id: string;
    externalId: string;
    name: string;
    isReservable: boolean;
}

export interface FloorRoomResource {
    id: string;
    externalId: string;
    name: string;
    isReservable: boolean;
    desks: FloorDeskResource[];
}

export interface FloorResources {
    floorId: string;
    rooms: FloorRoomResource[];
}
