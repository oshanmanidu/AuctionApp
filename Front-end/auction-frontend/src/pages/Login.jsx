import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        setError('');
        
        try {
            const res = await api.post('/auth/login', { username, password });
            const { token, role } = res.data;

            localStorage.setItem('token', token);
            localStorage.setItem('role', role);
            localStorage.setItem('username', username);

            if (role === 'Admin') {
                navigate('/admin');
            } else {
                navigate('/auctions');
            }
        } catch (err) {
            setError('Invalid credentials. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-background">
                <div className="auth-pattern"></div>
            </div>
            
            <div className="auth-card animate-fade-in">
                <div className="auth-header">
                    <div className="auth-logo">
                        <div className="logo-icon">🏛️</div>
                        <h1 className="logo-title">AuctionHub</h1>
                    </div>
                    <p className="auth-subtitle">Welcome back! Sign in to your account</p>
                </div>

                {error && (
                    <div className="alert alert-error animate-slide-in">
                        <span className="alert-icon">⚠️</span>
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="auth-form">
                    <div className="form-group">
                        <label className="form-label" htmlFor="username">
                            Username
                        </label>
                        <div className="input-wrapper">
                            <span className="input-icon">👤</span>
                            <input
                                id="username"
                                type="text"
                                className="form-input"
                                placeholder="Enter your username"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                required
                                disabled={isLoading}
                            />
                        </div>
                    </div>

                    <div className="form-group">
                        <label className="form-label" htmlFor="password">
                            Password
                        </label>
                        <div className="input-wrapper">
                            <span className="input-icon">🔒</span>
                            <input
                                id="password"
                                type="password"
                                className="form-input"
                                placeholder="Enter your password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                                disabled={isLoading}
                            />
                        </div>
                    </div>

                    <button 
                        type="submit" 
                        className="btn btn-primary btn-lg auth-submit"
                        disabled={isLoading}
                    >
                        {isLoading ? (
                            <>
                                <span className="loading"></span>
                                Signing In...
                            </>
                        ) : (
                            '🔑 Sign In'
                        )}
                    </button>
                </form>

                <div className="auth-footer">
                    <p className="auth-link-text">
                        Don't have an account?{' '}
                        <button
                            className="auth-link"
                            onClick={() => navigate('/register')}
                            disabled={isLoading}
                        >
                            Create one here
                        </button>
                    </p>
                </div>
            </div>
        </div>
    );
}