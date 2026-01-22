import { createContext } from 'react';

interface Toast {
    id: string;
    message: string;
    type: 'info' | 'success' | 'warning' | 'error';
}

export interface ToastContextType {
    showToast: (message: string, type?: Toast['type']) => void;
}

export const ToastContext = createContext<ToastContextType | undefined>(undefined);
