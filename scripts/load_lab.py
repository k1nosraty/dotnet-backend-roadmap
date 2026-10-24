"""Bounded closed-loop load for the local lesson-85 host only; JSON report and explicit budgets."""
from concurrent.futures import ThreadPoolExecutor
import argparse
import collections
import json
import math
from pathlib import Path
import time
from urllib import request, error, parse


def percentile(values, fraction):
    if not values: return None
    return sorted(values)[max(0,math.ceil(len(values)*fraction)-1)]

def validate_base(base):
    value=parse.urlsplit(base)
    if value.scheme!='http' or value.hostname not in ['localhost','127.0.0.1','::1'] or value.username or value.password or value.path not in ['', '/'] or value.query or value.fragment:
        raise ValueError('Use a loopback HTTP base URL with no path, credentials or query.')
    return base.rstrip('/')

def measure(base, algorithm, count, requests, concurrency):
    base=validate_base(base)
    if algorithm not in ['loop','formula'] or not 1<=count<=1_000_000 or not 1<=requests<=512 or not 1<=concurrency<=16:
        raise ValueError('Invalid bounded workload.')
    expected=count*(count+1)*(2*count+1)//6
    def fetch(path):
        opener=request.build_opener(request.ProxyHandler({}))
        with opener.open(base+path,timeout=10) as response:return response.status,json.load(response)
    path=f'/sum/{algorithm}?count={count}'
    for _ in range(5):
        status,body=fetch(path)
        if status!=200 or body.get('value')!=expected:raise ValueError('Warmup contract failure.')
    _,before=fetch('/runtime')
    def one(_):
        start=time.perf_counter(); status='transport-error'; success=False
        try:
            status,body=fetch(path);success=status==200 and body.get('value')==expected
        except error.HTTPError as ex:status=ex.code
        except (error.URLError,TimeoutError,ConnectionError):pass
        except (json.JSONDecodeError,ValueError):status='contract-error'
        return (time.perf_counter()-start)*1000,str(status),success
    started=time.perf_counter()
    with ThreadPoolExecutor(max_workers=concurrency) as pool:rows=list(pool.map(one,range(requests)))
    elapsed=time.perf_counter()-started
    _,after=fetch('/runtime')
    good=sum(row[2] for row in rows);latencies=[row[0] for row in rows]
    return {'algorithm':algorithm,'count':count,'requests':requests,'concurrency':concurrency,
        'successful':good,'failed':requests-good,'errorRate':(requests-good)/requests,
        'elapsedSeconds':elapsed,'successfulPerSecond':good/elapsed,
        'latencyAllMs':{'p50':percentile(latencies,.5),'p95':percentile(latencies,.95),'p99':percentile(latencies,.99)},
        'statuses':dict(collections.Counter(row[1] for row in rows)),
        'processCpuMs':after['cpuMs']-before['cpuMs'],'allocatedBytes':after['allocatedBytes']-before['allocatedBytes']}

def passes(report,p95,error_rate):return report['latencyAllMs']['p95']<=p95 and report['errorRate']<=error_rate

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base',default='http://127.0.0.1:5085')
    parser.add_argument('--algorithm',choices=['loop','formula'],default='loop')
    parser.add_argument('--count',type=int,default=1000000)
    parser.add_argument('--requests',type=int,default=64)
    parser.add_argument('--concurrency',type=int,default=4)
    parser.add_argument('--budget-p95-ms',type=float,default=200)
    parser.add_argument('--max-error-rate',type=float,default=0)
    parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    if args.budget_p95_ms<=0 or not 0<=args.max_error_rate<=1:parser.error('Invalid budget.')
    report=measure(args.base,args.algorithm,args.count,args.requests,args.concurrency)
    report['passed']=passes(report,args.budget_p95_ms,args.max_error_rate)
    text=json.dumps(report,indent=2);print(text)
    if args.output:
        if args.output.exists():raise SystemExit('Choose a new output filename; nothing is overwritten.')
        args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(text+'\n',encoding='utf-8')
    raise SystemExit(0 if report['passed'] else 1)
