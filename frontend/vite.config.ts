import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(),],
  define: { 'process.env': {}, },
  css: {
    preprocessorOptions: {
      scss: {
        // Konrix's vendored theme relies on the classic global-scope @import cascade across
        // ~29 partial files with no @use of their own — migrating to @use would need every
        // partial updated individually (real visual-regression risk) for a warning that's
        // otherwise harmless pre-Sass-3.0. Silenced rather than migrated.
        silenceDeprecations: ['import', 'legacy-js-api'],
      },
    },
  },
})
