/**
 * Used when the SPA is served from the same host as the API (e.g. nginx proxies `/api` to the backend).
 * Build: `ng build --configuration production-docker`
 */
export const environment = {
  production: true,
  /** Same-origin API — nginx (or your reverse proxy) forwards `/api` to the ASP.NET container */
  apiBaseUrl: '/api',
  defaultOrganizationSlug: 'default',
  clientDisplayName: 'ConsultancyManagement Solutions',
  clientTabTitle: 'consultancy-management'
};
