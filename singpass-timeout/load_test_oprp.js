import { browser } from 'k6/browser';
import { check } from 'k6';

export const options = {
  scenarios: {
    ui: {
      executor: 'shared-iterations',
      options: {
        browser: {
          type: 'chromium',
        },
      },
      vus: 10,
      iterations: 10,
      startTime: "0s",
    },
  },
  thresholds: {
    checks: ['rate==1.0'],
  },
};

export default async function () {

  const oprpUrl = "http://127.0.0.1:8080/eservices/broker/mp/auth";
  const oprpQS = "?scope=openid+offline_access&response_type=code&redirect_uri=http://127.0.0.1:8080/eservices/broker/mp/interaction/callback&state=eyJzdGF0ZSI6ImlpLUs2Y0VHLWc3ME0xV0VQUHN6aTctOWotdFZNbU1DbjJGTzFqMW1GYVEiLCJpc1FSIjpmYWxzZX0%3D&nonce=GhDBIoYfSOWsN5wwM9LSALMhpPPoHCI_hTxyVBkd8Rc&client_id=mockpass";
  const oprpFullUrl = oprpUrl + oprpQS;

  const context = await browser.newContext();
  await context.grantPermissions(['clipboard-read', 'clipboard-write'], {
    origin: oprpUrl,
  });

  const page = await context.newPage();

  try {

    console.log(`goto page ${oprpUrl}`)

    var resp = null;

    resp = await page.goto(oprpFullUrl);

    console.log(`at page ${page.url()}`)
    

    if (!resp) {
      console.error('page response is null');
    }

    const content = await resp.text();

    if (resp.status() != 200) {
      console.error(content);
    }
    
    
    check(resp, {
      'is login mockpass successful': (r) => r.status() == 200,
      'is login mockpass response message successful': (r) => content.toLowerCase() == 'mockpass login successfully'
    });

  } finally {
    await page.close();
  }
}
