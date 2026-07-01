import React from 'react';

import AllRoutes from "./routes/Routes";

import "nouislider/distribute/nouislider.css";

import "./assets/scss/app.scss";
import "./assets/scss/icons.scss";

// NOTE: the stock Konrix template calls configureFakeBackend() here, which installs a global
// axios-mock-adapter that intercepts every axios request in the app (mock and real alike) before
// it ever reaches the network — silently 404ing anything not explicitly mocked (like our real
// /auth/login). Removed now that a real backend exists; see fake-backend.ts if a mock is ever
// needed again for frontend-only development.

const App = () => {

  return (
    <>
      <React.Fragment>
        <AllRoutes />
      </React.Fragment>
    </>
  );
}

export default App;
