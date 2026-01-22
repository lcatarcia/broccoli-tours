import { useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import { ToastContext } from './ToastContext';
import './Toast.css';

interface Toast {
    id: string;
    message: string;
    type: 'info' | 'success' | 'warning' | 'error';
}

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);

    const showToast = useCallback((message: string, type: Toast['type'] = 'info') => {
        const id = `toast-${Date.now()}-${Math.random()}`;
        const newToast: Toast = { id, message, type };

        setToasts(prev => [...prev, newToast]);

        // Auto remove after 4 seconds
        setTimeout(() => {
            setToasts(prev => prev.filter(t => t.id !== id));
        }, 4000);
    }, []);

    const removeToast = useCallback((id: string) => {
        setToasts(prev => prev.filter(t => t.id !== id));
    }, []);

    return (
        <ToastContext.Provider value={{ showToast }}>
            {children}
            <div className="toast-container">
                {toasts.map(toast => (
                    <div
                        key={toast.id}
                        className={`toast toast-${toast.type}`}
                        onClick={() => removeToast(toast.id)}
                    >
                        <span className="toast-icon">
                            {toast.type === 'info' && 'ℹ️'}
                            {toast.type === 'success' && '✅'}
                            {toast.type === 'warning' && '⚠️'}
                            {toast.type === 'error' && '❌'}
                        </span>
                        <span className="toast-message">{toast.message}</span>
                    </div>
                ))}
            </div>
        </ToastContext.Provider>
    );
}
