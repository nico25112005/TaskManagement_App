/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: '#5E97D9',
        danger: '#e35d48',
        highlight: '#FFC941',
        success: '#509C6E',
        bg: '#ededed',
        panel: '#ffffff',
        border: '#abadb3',
        hover: '#FFEFD9',
        today: '#FFFCF1D9',
        'dark-bg': '#1a1a2e',
        'dark-panel': '#252540',
        'dark-border': '#3a3a5c',
        'dark-text': '#e0e0e0',
      },
      fontFamily: {
        sans: ["Bahnschrift", 'Segoe UI', 'system-ui', 'sans-serif'],
      },
      animation: {
        'fade-in': 'fadeIn 0.2s ease-in-out',
        'slide-up': 'slideUp 0.3s ease-out',
        'pulse-soft': 'pulseSoft 2s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { transform: 'translateY(10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
      },
    },
  },
  plugins: [],
}